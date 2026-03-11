using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TodoApp.Api.Data;
using TodoApp.Api.Data.Entities;
using TodoApp.Shared.Models;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Tests;

public class AuthMeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "dev-only-jwt-secret-key-minimum-32-characters-long!!";
    private const string JwtIssuer = "TodoApp";
    private const string JwtAudience = "TodoApp";

    private readonly WebApplicationFactory<Program> _factory;

    public AuthMeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // 既存の DbContext 登録を削除して InMemory SQLite に差し替え
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TodoAppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
                connection.Open();

                services.AddDbContext<TodoAppDbContext>(options =>
                    options.UseSqlite(connection));

                // DB を初期化してテストユーザーを作成
                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
                db.Database.EnsureCreated();
                db.Users.Add(new User
                {
                    Id = 1,
                    Email = "test@example.com",
                    DisplayName = "テストユーザー",
                    PasswordHash = "dummy-hash",
                    Role = UserRole.Member
                });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task 認証なしでアクセスすると401が返る()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 認証済みユーザーでアクセスすると200とユーザー情報が返る()
    {
        var client = _factory.CreateClient();
        var token = GenerateTestToken(userId: "1", email: "test@example.com");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("テストユーザー", user.DisplayName);
        Assert.Equal(UserRole.Member, user.Role);
    }

    [Fact]
    public async Task 存在しないユーザーIDの場合404が返る()
    {
        var client = _factory.CreateClient();
        var token = GenerateTestToken(userId: "999", email: "unknown@example.com");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static string GenerateTestToken(string userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Email, email)
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
