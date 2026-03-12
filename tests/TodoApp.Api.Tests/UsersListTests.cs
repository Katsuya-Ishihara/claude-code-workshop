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

public class UsersListTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly SqliteConnection _connection;

    public UsersListTests(WebApplicationFactory<Program> factory)
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

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        db.Database.EnsureCreated();

        db.Users.AddRange(
            new User
            {
                Email = "alice@example.com",
                DisplayName = "Alice",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.Admin
            },
            new User
            {
                Email = "bob@example.com",
                DisplayName = "Bob",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Role = UserRole.Member
            });
        db.SaveChanges();
    }

    private async Task AuthenticateAsync()
    {
        var loginRequest = new LoginRequest
        {
            Email = "alice@example.com",
            Password = "Password123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResult!.Token);
    }

    [Fact]
    public async Task 認証済みユーザーがユーザー一覧を取得すると200が返る()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>();
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task 未認証でユーザー一覧を取得すると401が返る()
    {
        var response = await _client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ユーザー一覧にはId_Email_DisplayName_Role_CreatedAtが含まれる()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/users");
        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>();

        Assert.NotNull(users);
        var alice = users.First(u => u.Email == "alice@example.com");
        Assert.True(alice.Id > 0);
        Assert.Equal("Alice", alice.DisplayName);
        Assert.Equal(UserRole.Admin, alice.Role);
        Assert.True(alice.CreatedAt <= DateTime.UtcNow);

        var bob = users.First(u => u.Email == "bob@example.com");
        Assert.True(bob.Id > 0);
        Assert.Equal("Bob", bob.DisplayName);
        Assert.Equal(UserRole.Member, bob.Role);
    }

    [Fact]
    public async Task ユーザー一覧はId昇順で返される()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/users");
        var users = await response.Content.ReadFromJsonAsync<List<UserSummaryResponse>>();

        Assert.NotNull(users);
        Assert.True(users.Count >= 2);
        for (int i = 1; i < users.Count; i++)
        {
            Assert.True(users[i].Id > users[i - 1].Id);
        }
    }

    [Fact]
    public async Task ユーザー一覧のレスポンスにパスワード情報が含まれない()
    {
        await AuthenticateAsync();

        var response = await _client.GetAsync("/api/users");
        var rawJson = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("password", rawJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("passwordHash", rawJson, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _connection.Dispose();
    }
}
