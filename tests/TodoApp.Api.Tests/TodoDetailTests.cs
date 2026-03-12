using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Api.Data;
using TodoApp.Api.Data.Entities;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Tests;

public class TodoDetailTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SqliteConnection _connection;

    public TodoDetailTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TodoAppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddDbContext<TodoAppDbContext>(options =>
                    options.UseSqlite(_connection));
            });
        });

        _client = _factory.CreateClient();

        // テスト用データをセットアップ
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        db.Database.EnsureCreated();

        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
        };
        var assignee = new User
        {
            Email = "assignee@example.com",
            DisplayName = "担当者",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
        };
        db.Users.AddRange(user, assignee);
        db.SaveChanges();

        var todo = new TodoItem
        {
            Title = "テストTodo",
            Description = "テスト用の説明",
            Status = TodoStatus.InProgress,
            Priority = Priority.High,
            ProgressRate = 50,
            DueDate = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            CreatedByUserId = user.Id,
            AssignedToUserId = assignee.Id
        };
        db.TodoItems.Add(todo);
        db.SaveChanges();
    }

    private async Task<string> GetTokenAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResult!.Token!;
    }

    [Fact]
    public async Task 存在するTodoのIDを指定すると200で詳細が返る()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/todos/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var todo = await response.Content.ReadFromJsonAsync<TodoResponse>();
        Assert.NotNull(todo);
        Assert.Equal(1, todo.Id);
        Assert.Equal("テストTodo", todo.Title);
        Assert.Equal("テスト用の説明", todo.Description);
        Assert.Equal(TodoStatus.InProgress, todo.Status);
        Assert.Equal(Priority.High, todo.Priority);
        Assert.Equal(50, todo.ProgressRate);
        Assert.Equal("テストユーザー", todo.CreatedByDisplayName);
        Assert.Equal("担当者", todo.AssignedToDisplayName);
    }

    [Fact]
    public async Task 存在しないTodoのIDを指定すると404が返る()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/todos/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task 認証なしでアクセスすると401が返る()
    {
        var response = await _client.GetAsync("/api/todos/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }
}
