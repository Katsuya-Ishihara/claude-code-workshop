using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Api.Data.Entities;
using TodoApp.Shared.Models;

namespace TodoApp.Api.Tests;

public class TodoAppDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TodoAppDbContext _context;

    public TodoAppDbContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TodoAppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TodoAppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Userを作成して取得できる()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            PasswordHash = "hashed"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var savedUser = await _context.Users.FirstAsync();
        Assert.Equal("test@example.com", savedUser.Email);
        Assert.NotEqual(default, savedUser.CreatedAt);
    }

    [Fact]
    public async Task TodoItemを作成して取得できる()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            PasswordHash = "hashed"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var todo = new TodoItem
        {
            Title = "テストタスク",
            CreatedByUserId = user.Id
        };
        _context.TodoItems.Add(todo);
        await _context.SaveChangesAsync();

        var savedTodo = await _context.TodoItems.Include(t => t.CreatedBy).FirstAsync();
        Assert.Equal("テストタスク", savedTodo.Title);
        Assert.Equal(TodoStatus.NotStarted, savedTodo.Status);
        Assert.Equal("テストユーザー", savedTodo.CreatedBy.DisplayName);
    }

    [Fact]
    public async Task 論理削除されたTodoはクエリから除外される()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            PasswordHash = "hashed"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var todo = new TodoItem
        {
            Title = "削除予定",
            CreatedByUserId = user.Id,
            DeletedAt = DateTime.UtcNow
        };
        _context.TodoItems.Add(todo);
        await _context.SaveChangesAsync();

        var todos = await _context.TodoItems.ToListAsync();
        Assert.Empty(todos);
    }

    [Fact]
    public async Task タイムスタンプが自動設定される()
    {
        var user = new User
        {
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            PasswordHash = "hashed"
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        Assert.NotEqual(default, user.CreatedAt);
        Assert.NotEqual(default, user.UpdatedAt);
        Assert.Equal(user.CreatedAt, user.UpdatedAt);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
