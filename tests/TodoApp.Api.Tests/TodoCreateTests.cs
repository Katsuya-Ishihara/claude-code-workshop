using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public class TodoCreateTests : IClassFixture<TodoCreateTests.TodoWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoWebAppFactory _factory;

    public TodoCreateTests(TodoWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var uniqueEmail = $"todo-test-{Guid.NewGuid()}@example.com";
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
        var uniqueEmail = $"todo-test-{Guid.NewGuid()}@example.com";
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

    [Fact]
    public async Task 正常なリクエストでTodoを作成できる_201を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "テストタスク",
            Description = "テストの説明",
            Priority = Priority.High,
            DueDate = DateTime.UtcNow.AddDays(7)
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.True(body.Id > 0);
        Assert.Equal("テストタスク", body.Title);
        Assert.Equal("テストの説明", body.Description);
        Assert.Equal(Priority.High, body.Priority);
        Assert.Equal(TodoStatus.NotStarted, body.Status);
        Assert.Equal(0, body.ProgressRate);
        Assert.NotNull(body.DueDate);
        Assert.True(body.CreatedByUserId > 0);
        Assert.Null(body.AssignedToUserId);
    }

    [Fact]
    public async Task タイトルのみでTodoを作成できる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "最小限のタスク"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal("最小限のタスク", body.Title);
        Assert.Null(body.Description);
        Assert.Equal(Priority.Medium, body.Priority);
        Assert.Null(body.DueDate);
        Assert.Null(body.AssignedToUserId);
    }

    [Fact]
    public async Task 担当者を指定してTodoを作成できる()
    {
        var (token, userId) = await GetAuthTokenWithUserIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "担当者付きタスク",
            AssignedToUserId = userId
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.Equal(userId, body.AssignedToUserId);
    }

    [Fact]
    public async Task 存在しない担当者を指定_404を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "存在しない担当者タスク",
            AssignedToUserId = 99999
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task タイトル未指定_400を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "",
            Description = "説明のみ"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", content);
    }

    [Fact]
    public async Task タイトルが200文字超_400を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = new string('あ', 201)
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Title", content);
    }

    [Fact]
    public async Task 未認証でアクセス_401を返す()
    {
        // Authorization ヘッダーなし
        var client = _factory.CreateClient();
        var request = new CreateTodoRequest
        {
            Title = "未認証タスク"
        };

        var response = await client.PostAsJsonAsync("/api/todos", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 作成したTodoのCreatedAtとUpdatedAtが設定される()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);

        var request = new CreateTodoRequest
        {
            Title = "タイムスタンプ確認タスク"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);
        Assert.True(body.CreatedAt >= beforeCreate);
        Assert.True(body.UpdatedAt >= beforeCreate);
    }

    [Fact]
    public async Task Locationヘッダーが正しく設定される()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "Locationヘッダー確認"
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);

        var locationHeader = response.Headers.Location;
        Assert.NotNull(locationHeader);
        Assert.Contains($"/api/todos/{body.Id}", locationHeader.ToString());
    }

    [Fact]
    public async Task DBに正しく保存される()
    {
        var (token, userId) = await GetAuthTokenWithUserIdAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new CreateTodoRequest
        {
            Title = "DB保存確認タスク",
            Description = "DB保存の説明",
            Priority = Priority.Low,
            DueDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc)
        };

        var response = await _client.PostAsJsonAsync("/api/todos", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(body);

        // DBから直接確認
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        var todoItem = await dbContext.TodoItems.FindAsync(body.Id);

        Assert.NotNull(todoItem);
        Assert.Equal("DB保存確認タスク", todoItem.Title);
        Assert.Equal("DB保存の説明", todoItem.Description);
        Assert.Equal(Priority.Low, todoItem.Priority);
        Assert.Equal(TodoStatus.NotStarted, todoItem.Status);
        Assert.Equal(0, todoItem.ProgressRate);
        Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc), todoItem.DueDate);
        Assert.Equal(userId, todoItem.CreatedByUserId);
        Assert.Null(todoItem.AssignedToUserId);
        Assert.Null(todoItem.DeletedAt);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class TodoWebAppFactory : WebApplicationFactory<Program>
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
