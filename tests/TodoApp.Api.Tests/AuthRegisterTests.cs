using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Api.Data;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Tests;

public class AuthRegisterTests : IClassFixture<AuthRegisterTests.AuthWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly AuthWebAppFactory _factory;

    public AuthRegisterTests(AuthWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task 正常なリクエストで登録できる_201を返す()
    {
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            DisplayName = "テストユーザー"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.Equal("test@example.com", body.Email);
        Assert.Equal("テストユーザー", body.DisplayName);
        Assert.True(body.UserId > 0);
    }

    [Fact]
    public async Task パスワードがハッシュ化されて保存される()
    {
        var request = new RegisterRequest
        {
            Email = "hash-check@example.com",
            Password = "password123",
            DisplayName = "ハッシュ確認"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // DBからユーザーを取得してパスワードがハッシュ化されていることを確認
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == "hash-check@example.com");

        Assert.NotNull(user);
        Assert.NotEqual("password123", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash));
    }

    [Fact]
    public async Task 重複するメールアドレスで登録_409Conflictを返す()
    {
        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "password123",
            DisplayName = "最初のユーザー"
        };

        // 1回目: 成功
        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // 2回目: 重複
        request.DisplayName = "2番目のユーザー";
        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var content = await second.Content.ReadAsStringAsync();
        var problemDetails = JsonDocument.Parse(content);
        Assert.Equal(409, problemDetails.RootElement.GetProperty("status").GetInt32());
    }

    [Theory]
    [InlineData("", "password123", "テスト", "Email")]
    [InlineData("invalid-email", "password123", "テスト", "Email")]
    [InlineData("test@example.com", "", "テスト", "Password")]
    [InlineData("test@example.com", "short", "テスト", "Password")]
    [InlineData("test@example.com", "password123", "", "DisplayName")]
    public async Task バリデーションエラー_400を返す(string email, string password, string displayName, string expectedField)
    {
        var request = new RegisterRequest
        {
            Email = email,
            Password = password,
            DisplayName = displayName
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains(expectedField, content);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class AuthWebAppFactory : WebApplicationFactory<Program>
    {
        private SqliteConnection? _connection;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // 既存のDbContextOptionsを削除
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TodoAppDbContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // InMemory SQLite を使用
                _connection = new SqliteConnection("DataSource=:memory:");
                _connection.Open();

                services.AddDbContext<TodoAppDbContext>(options =>
                    options.UseSqlite(_connection));

                // DBを作成
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
