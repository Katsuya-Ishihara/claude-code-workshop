using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Api.Data;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Tests;

public class TodoAssignTests : IClassFixture<TodoAssignTests.TodoAssignWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoAssignWebAppFactory _factory;

    public TodoAssignTests(TodoAssignWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var uniqueEmail = $"assign-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "password123",
            DisplayName = "テストユーザー"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();

        var loginRequest = new LoginRequest
        {
            Email = uniqueEmail,
            Password = "password123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token!;
    }

    private async Task<(string Token, int UserId)> GetAuthTokenWithUserIdAsync()
    {
        var uniqueEmail = $"assign-test-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = uniqueEmail,
            Password = "password123",
            DisplayName = "テストユーザー"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var loginRequest = new LoginRequest
        {
            Email = uniqueEmail,
            Password = "password123"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return (authResponse!.Token!, registerBody!.UserId);
    }

    private async Task<TodoResponse> CreateTodoAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "担当者変更テスト用タスク"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        return body!;
    }

    [Fact]
    public async Task 担当者を変更できる_200を返す()
    {
        var (token1, _) = await GetAuthTokenWithUserIdAsync();
        var (_, user2Id) = await GetAuthTokenWithUserIdAsync();

        var todo = await CreateTodoAsync(token1);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var assignRequest = new UpdateTodoAssigneeRequest
        {
            AssignedToUserId = user2Id
        };

        var response = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/assign", assignRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(todo.Id, body.Id);
        Assert.Equal(user2Id, body.AssignedToUserId);
    }

    [Fact]
    public async Task 担当者を解除できる_nullを指定()
    {
        var (token1, user1Id) = await GetAuthTokenWithUserIdAsync();

        var todo = await CreateTodoAsync(token1);

        // まず担当者を設定
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var assignRequest = new UpdateTodoAssigneeRequest
        {
            AssignedToUserId = user1Id
        };
        var assignResponse = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/assign", assignRequest);
        assignResponse.EnsureSuccessStatusCode();

        // 担当者を解除（nullを指定）
        var unassignRequest = new UpdateTodoAssigneeRequest
        {
            AssignedToUserId = null
        };
        var response = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/assign", unassignRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(todo.Id, body.Id);
        Assert.Null(body.AssignedToUserId);
    }

    [Fact]
    public async Task 存在しないユーザーを担当者に指定した場合_404を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var assignRequest = new UpdateTodoAssigneeRequest
        {
            AssignedToUserId = 99999
        };

        var response = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/assign", assignRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 存在しないTodoの担当者を変更しようとした場合_404を返す()
    {
        var (token, userId) = await GetAuthTokenWithUserIdAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var assignRequest = new UpdateTodoAssigneeRequest
        {
            AssignedToUserId = userId
        };

        var response = await _client.PatchAsJsonAsync("/api/todos/99999/assign", assignRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 未認証の場合_401を返す()
    {
        var client = _factory.CreateClient();
        var assignRequest = new UpdateTodoAssigneeRequest
        {
            AssignedToUserId = 1
        };

        var response = await client.PatchAsJsonAsync("/api/todos/1/assign", assignRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class TodoAssignWebAppFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TodoAppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<TodoAppDbContext>(options =>
                    options.UseSqlite(_connection));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
                db.Database.EnsureCreated();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _connection?.Dispose();
        }
    }
}
