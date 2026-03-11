using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Api.Data;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Tests;

public class TodoUpdateTests : IClassFixture<TodoUpdateTests.TodoUpdateWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoUpdateWebAppFactory _factory;

    public TodoUpdateTests(TodoUpdateWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var uniqueEmail = $"todo-update-test-{Guid.NewGuid()}@example.com";
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
        var uniqueEmail = $"todo-update-test-{Guid.NewGuid()}@example.com";
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
            Title = "更新前タスク",
            Description = "更新前の説明",
            Priority = Priority.Low,
            DueDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        return body!;
    }

    [Fact]
    public async Task 正常なリクエストでTodoを更新できる_200を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        var updateRequest = new UpdateTodoRequest
        {
            Title = "更新後タスク",
            Description = "更新後の説明",
            Priority = Priority.High,
            DueDate = new DateTime(2027, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(todo.Id, body.Id);
        Assert.Equal("更新後タスク", body.Title);
        Assert.Equal("更新後の説明", body.Description);
        Assert.Equal(Priority.High, body.Priority);
        Assert.NotNull(body.DueDate);
        Assert.Equal(new DateTime(2027, 6, 15, 0, 0, 0, DateTimeKind.Utc), body.DueDate);
    }

    [Fact]
    public async Task タイトルが空の場合_400を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        var updateRequest = new UpdateTodoRequest
        {
            Title = "",
            Description = "説明"
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", content);
    }

    [Fact]
    public async Task タイトルが200文字超の場合_400を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        var updateRequest = new UpdateTodoRequest
        {
            Title = new string('あ', 201)
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", content);
    }

    [Fact]
    public async Task 存在しないTodoを更新しようとした場合_404を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateTodoRequest
        {
            Title = "存在しないTodo"
        };

        var response = await _client.PutAsJsonAsync("/api/todos/99999", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 存在しない担当者を指定した場合_404を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        var updateRequest = new UpdateTodoRequest
        {
            Title = "担当者不明タスク",
            AssignedToUserId = 99999
        };

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PutAsJsonAsync($"/api/todos/{todo.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 未認証の場合_401を返す()
    {
        var client = _factory.CreateClient();

        var updateRequest = new UpdateTodoRequest
        {
            Title = "未認証タスク"
        };

        var response = await client.PutAsJsonAsync("/api/todos/1", updateRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 担当者をnullに変更できる()
    {
        var (token, userId) = await GetAuthTokenWithUserIdAsync();

        // 担当者付きでTodoを作成
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var createRequest = new CreateTodoRequest
        {
            Title = "担当者付きタスク",
            AssignedToUserId = userId
        };
        var createResponse = await _client.PostAsJsonAsync("/api/todos", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdTodo = await createResponse.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(createdTodo);
        Assert.Equal(userId, createdTodo.AssignedToUserId);

        // 担当者をnullに更新
        var updateRequest = new UpdateTodoRequest
        {
            Title = "担当者付きタスク",
            AssignedToUserId = null
        };

        var response = await _client.PutAsJsonAsync($"/api/todos/{createdTodo.Id}", updateRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Null(body.AssignedToUserId);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class TodoUpdateWebAppFactory : WebApplicationFactory<Program>
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
