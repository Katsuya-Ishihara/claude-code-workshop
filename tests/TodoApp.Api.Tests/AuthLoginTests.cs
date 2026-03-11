using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Api.Data;
using TodoApp.Api.Data.Entities;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Tests;

public class AuthLoginTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SqliteConnection _connection;

    public AuthLoginTests(WebApplicationFactory<Program> factory)
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

        // テスト用ユーザーを事前登録
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        db.Database.EnsureCreated();
        db.Users.Add(new User
        {
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task 正しいメールとパスワードでログインするとトークンが返る()
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task 存在しないメールでログインすると401が返る()
    {
        var request = new LoginRequest
        {
            Email = "notfound@example.com",
            Password = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 間違ったパスワードでログインすると401が返る()
    {
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ログインで取得したトークンで認証付きエンドポイントにアクセスできる()
    {
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(authResult);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.Token);

        var authResponse = await _client.GetAsync("/test/auth");
        Assert.Equal(HttpStatusCode.OK, authResponse.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }
}
