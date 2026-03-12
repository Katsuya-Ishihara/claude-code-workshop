using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

public class TodoStatusChangeTests : IClassFixture<TodoStatusChangeTests.TodoStatusWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoStatusWebAppFactory _factory;

    public TodoStatusChangeTests(TodoStatusWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var uniqueEmail = $"status-test-{Guid.NewGuid()}@example.com";
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

    private async Task<TodoResponse> CreateTodoAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "ステータス変更テスト用タスク"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        return body!;
    }

    [Fact]
    public async Task NotStartedからInProgressに変更できる()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateTodoStatusRequest
        {
            Status = TodoStatus.InProgress
        };

        var response = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/status", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(TodoStatus.InProgress, body.Status);
    }

    [Fact]
    public async Task InProgressからCompletedに変更するとCompletedAtが自動設定される()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // まずInProgressに変更
        var inProgressRequest = new UpdateTodoStatusRequest { Status = TodoStatus.InProgress };
        var inProgressResponse = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/status", inProgressRequest);
        inProgressResponse.EnsureSuccessStatusCode();

        // CompletedAt設定前の時刻を記録
        var beforeComplete = DateTime.UtcNow.AddSeconds(-1);

        // Completedに変更
        var completedRequest = new UpdateTodoStatusRequest { Status = TodoStatus.Completed };
        var completedResponse = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/status", completedRequest);

        Assert.Equal(HttpStatusCode.OK, completedResponse.StatusCode);

        var body = await completedResponse.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(TodoStatus.Completed, body.Status);

        // DBから直接CompletedAtを確認
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        var todoItem = await db.TodoItems.FindAsync(todo.Id);
        Assert.NotNull(todoItem);
        Assert.NotNull(todoItem.CompletedAt);
        Assert.True(todoItem.CompletedAt >= beforeComplete);
    }

    [Fact]
    public async Task CompletedからInProgressに戻すとCompletedAtがnullになる()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Completedに変更
        var completedRequest = new UpdateTodoStatusRequest { Status = TodoStatus.Completed };
        var completedResponse = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/status", completedRequest);
        completedResponse.EnsureSuccessStatusCode();

        // DBでCompletedAtが設定されていることを確認
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var todoItem = await db.TodoItems.FindAsync(todo.Id);
            Assert.NotNull(todoItem!.CompletedAt);
        }

        // InProgressに戻す
        var inProgressRequest = new UpdateTodoStatusRequest { Status = TodoStatus.InProgress };
        var inProgressResponse = await _client.PatchAsJsonAsync($"/api/todos/{todo.Id}/status", inProgressRequest);

        Assert.Equal(HttpStatusCode.OK, inProgressResponse.StatusCode);

        var body = await inProgressResponse.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(TodoStatus.InProgress, body.Status);

        // DBからCompletedAtがnullであることを確認
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var todoItem = await db.TodoItems.FindAsync(todo.Id);
            Assert.NotNull(todoItem);
            Assert.Null(todoItem.CompletedAt);
        }
    }

    [Fact]
    public async Task 存在しないTodoのステータスを変更しようとした場合_404を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new UpdateTodoStatusRequest { Status = TodoStatus.InProgress };
        var response = await _client.PatchAsJsonAsync("/api/todos/99999/status", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 不正なステータス値の場合_400を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var json = new StringContent("{\"status\": 99}", Encoding.UTF8, "application/json");
        var response = await _client.PatchAsync($"/api/todos/{todo.Id}/status", json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task 未認証の場合_401を返す()
    {
        var client = _factory.CreateClient();
        var request = new UpdateTodoStatusRequest { Status = TodoStatus.InProgress };
        var response = await client.PatchAsJsonAsync("/api/todos/1/status", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class TodoStatusWebAppFactory : WebApplicationFactory<Program>
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
