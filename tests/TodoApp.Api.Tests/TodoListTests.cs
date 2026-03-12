using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
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

public class TodoListTests : IClassFixture<TodoListTests.TodoListWebAppFactory>, IDisposable
{
    private readonly HttpClient _client;
    private readonly TodoListWebAppFactory _factory;

    public TodoListTests(TodoListWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        var registerRequest = new RegisterRequest
        {
            Email = $"todo-test-{Guid.NewGuid()}@example.com",
            Password = "password123",
            DisplayName = "テストユーザー"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        // ログインしてトークンを取得
        var loginRequest = new LoginRequest
        {
            Email = registerRequest.Email,
            Password = "password123"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return loginBody!.Token!;
    }

    private async Task SeedTodosAsync(int createdByUserId, int count = 5)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();

        for (var i = 0; i < count; i++)
        {
            db.TodoItems.Add(new TodoItem
            {
                Title = $"テストTodo {i + 1}",
                Description = i % 2 == 0 ? $"説明文 {i + 1}" : null,
                Status = (TodoStatus)(i % 3),
                Priority = (Priority)(i % 3),
                CreatedByUserId = createdByUserId,
                DueDate = DateTime.UtcNow.AddDays(i)
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<int> CreateUserAndSeedTodosAsync(int count = 5)
    {
        // ユーザーを作成
        var email = $"seed-{Guid.NewGuid()}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "password123",
            DisplayName = "シードユーザー"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        var user = await db.Users.FirstAsync(u => u.Email == email);

        await SeedTodosAsync(user.Id, count);
        return user.Id;
    }

    [Fact]
    public async Task 未認証でアクセス_401を返す()
    {
        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task 認証済みでTodo一覧を取得できる_200を返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateUserAndSeedTodosAsync();

        var response = await _client.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Items.Count > 0);
        Assert.True(body.TotalCount > 0);
        Assert.Equal(1, body.Page);
        Assert.Equal(10, body.PageSize);
    }

    [Fact]
    public async Task ページネーションが正しく動作する()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateUserAndSeedTodosAsync(15);

        // 1ページ目
        var response1 = await _client.GetAsync("/api/todos?page=1&pageSize=5");
        var body1 = await response1.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body1);
        Assert.Equal(5, body1.Items.Count);
        Assert.Equal(1, body1.Page);
        Assert.Equal(5, body1.PageSize);
        Assert.True(body1.HasNextPage);
        Assert.False(body1.HasPreviousPage);

        // 2ページ目
        var response2 = await _client.GetAsync("/api/todos?page=2&pageSize=5");
        var body2 = await response2.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body2);
        Assert.Equal(5, body2.Items.Count);
        Assert.Equal(2, body2.Page);
        Assert.True(body2.HasPreviousPage);
    }

    [Fact]
    public async Task ステータスでフィルタできる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateUserAndSeedTodosAsync(9);

        var response = await _client.GetAsync("/api/todos?status=0");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.All(body.Items, item => Assert.Equal(TodoStatus.NotStarted, item.Status));
    }

    [Fact]
    public async Task 優先度でフィルタできる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateUserAndSeedTodosAsync(9);

        var response = await _client.GetAsync("/api/todos?priority=2");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.All(body.Items, item => Assert.Equal(Priority.High, item.Priority));
    }

    [Fact]
    public async Task 担当者でフィルタできる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 担当者付きTodoを作成
        var email = $"assignee-{Guid.NewGuid()}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "password123",
            DisplayName = "担当者"
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
        var assignee = await db.Users.FirstAsync(u => u.Email == email);

        // 別ユーザーでTodoを作成（担当者を設定）
        var creator = await db.Users.FirstAsync(u => u.Email != email);
        db.TodoItems.Add(new TodoItem
        {
            Title = "担当者付きTodo",
            CreatedByUserId = creator.Id,
            AssignedToUserId = assignee.Id
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/todos?assignedToUserId={assignee.Id}");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.All(body.Items, item => Assert.Equal(assignee.Id, item.AssignedToUserId));
    }

    [Fact]
    public async Task キーワードでタイトルを検索できる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueKeyword = $"ユニーク{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var user = await db.Users.FirstAsync();
            db.TodoItems.Add(new TodoItem
            {
                Title = $"{uniqueKeyword}のタスク",
                CreatedByUserId = user.Id
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/todos?keyword={Uri.EscapeDataString(uniqueKeyword)}");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Contains(uniqueKeyword, body.Items[0].Title);
    }

    [Fact]
    public async Task キーワードで説明文を検索できる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueKeyword = $"説明検索{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var user = await db.Users.FirstAsync();
            db.TodoItems.Add(new TodoItem
            {
                Title = "通常のタイトル",
                Description = $"これは{uniqueKeyword}です",
                CreatedByUserId = user.Id
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/todos?keyword={Uri.EscapeDataString(uniqueKeyword)}");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.Contains(uniqueKeyword, body.Items[0].Description);
    }

    [Fact]
    public async Task ソートが正しく動作する_タイトル昇順()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var prefix = Guid.NewGuid().ToString("N");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var user = await db.Users.FirstAsync();
            db.TodoItems.Add(new TodoItem { Title = $"{prefix}_B_タスク", CreatedByUserId = user.Id });
            db.TodoItems.Add(new TodoItem { Title = $"{prefix}_A_タスク", CreatedByUserId = user.Id });
            db.TodoItems.Add(new TodoItem { Title = $"{prefix}_C_タスク", CreatedByUserId = user.Id });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/todos?keyword={prefix}&sortBy=title&sortDesc=false");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Equal(3, body.Items.Count);
        Assert.Equal($"{prefix}_A_タスク", body.Items[0].Title);
        Assert.Equal($"{prefix}_B_タスク", body.Items[1].Title);
        Assert.Equal($"{prefix}_C_タスク", body.Items[2].Title);
    }

    [Fact]
    public async Task ソートが正しく動作する_タイトル降順()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var prefix = Guid.NewGuid().ToString("N");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var user = await db.Users.FirstAsync();
            db.TodoItems.Add(new TodoItem { Title = $"{prefix}_B_タスク", CreatedByUserId = user.Id });
            db.TodoItems.Add(new TodoItem { Title = $"{prefix}_A_タスク", CreatedByUserId = user.Id });
            db.TodoItems.Add(new TodoItem { Title = $"{prefix}_C_タスク", CreatedByUserId = user.Id });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/todos?keyword={prefix}&sortBy=title&sortDesc=true");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Equal(3, body.Items.Count);
        Assert.Equal($"{prefix}_C_タスク", body.Items[0].Title);
        Assert.Equal($"{prefix}_B_タスク", body.Items[1].Title);
        Assert.Equal($"{prefix}_A_タスク", body.Items[2].Title);
    }

    [Fact]
    public async Task 論理削除されたTodoは表示されない()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueTitle = $"削除済み{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var user = await db.Users.FirstAsync();
            db.TodoItems.Add(new TodoItem
            {
                Title = uniqueTitle,
                CreatedByUserId = user.Id,
                DeletedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/todos?keyword={Uri.EscapeDataString(uniqueTitle)}");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
    }

    [Fact]
    public async Task レスポンスに作成者と担当者の表示名が含まれる()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueTitle = $"表示名確認{Guid.NewGuid():N}";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();
            var user = await db.Users.FirstAsync();
            db.TodoItems.Add(new TodoItem
            {
                Title = uniqueTitle,
                CreatedByUserId = user.Id,
                AssignedToUserId = user.Id
            });
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/api/todos?keyword={Uri.EscapeDataString(uniqueTitle)}");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Single(body.Items);
        Assert.NotEmpty(body.Items[0].CreatedByDisplayName);
        Assert.NotNull(body.Items[0].AssignedToDisplayName);
    }

    [Fact]
    public async Task PageSizeの上限が100に制限される()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/todos?pageSize=200");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Equal(100, body.PageSize);
    }

    [Fact]
    public async Task Todoが0件の場合空のリストを返す()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uniqueKeyword = $"存在しない{Guid.NewGuid():N}";
        var response = await _client.GetAsync($"/api/todos?keyword={Uri.EscapeDataString(uniqueKeyword)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body.Items);
        Assert.Equal(0, body.TotalCount);
    }

    [Fact]
    public async Task PaginatedResponseのTotalPagesが正しく計算される()
    {
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateUserAndSeedTodosAsync(12);

        var response = await _client.GetAsync("/api/todos?pageSize=5");
        var body = await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>();
        Assert.NotNull(body);
        // TotalPages は TotalCount / PageSize の切り上げ
        Assert.True(body.TotalPages >= 3); // 12件以上あるので5件ずつなら3ページ以上
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    public class TodoListWebAppFactory : WebApplicationFactory<Program>
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
