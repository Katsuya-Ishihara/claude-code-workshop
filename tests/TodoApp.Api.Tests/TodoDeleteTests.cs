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

public class TodoDeleteTests : IClassFixture<TodoDeleteTests.TodoDeleteWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoDeleteWebAppFactory _factory;

    public TodoDeleteTests(TodoDeleteWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var uniqueEmail = $"todo-delete-test-{Guid.NewGuid()}@example.com";
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
            Title = $"削除テスト用タスク-{Guid.NewGuid()}"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        return body!;
    }

    [Fact]
    public async Task 正常なリクエストでTodoを削除できる_204を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.DeleteAsync($"/api/todos/{todo.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task 削除済みTodoが一覧から除外される()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var deleteResponse = await _client.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // DBから直接確認（グローバルクエリフィルタにより論理削除済みは除外される）
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        var deletedTodo = await db.TodoItems.FindAsync(todo.Id);
        Assert.Null(deletedTodo);
    }

    [Fact]
    public async Task 存在しないTodoを削除しようとした場合_404を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.DeleteAsync("/api/todos/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 未認証の場合_401を返す()
    {
        var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/todos/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 削除済みTodoを再度削除しようとした場合_404を返す()
    {
        var token = await GetAuthTokenAsync();
        var todo = await CreateTodoAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1回目の削除
        var firstDelete = await _client.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, firstDelete.StatusCode);

        // 2回目の削除（論理削除済みなのでグローバルフィルタにより404）
        var secondDelete = await _client.DeleteAsync($"/api/todos/{todo.Id}");
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class TodoDeleteWebAppFactory : WebApplicationFactory<Program>
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
