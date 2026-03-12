using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoApp.Client.Pages;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Pages;

public class TodoEditTests : TestContext
{
    private readonly Mock<ITodoApiClient> _mockTodoApi;
    private readonly Mock<IUserApiClient> _mockUserApi;

    public TodoEditTests()
    {
        _mockTodoApi = new Mock<ITodoApiClient>();
        _mockUserApi = new Mock<IUserApiClient>();
        Services.AddSingleton(_mockTodoApi.Object);
        Services.AddSingleton(_mockUserApi.Object);

        _mockUserApi
            .Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<UserResponse>
            {
                new(1, "user1@test.com", "User 1", UserRole.Member),
                new(2, "user2@test.com", "User 2", UserRole.Member)
            });
    }

    [Fact]
    public void ページタイトルが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _mockTodoApi.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        Assert.Contains("Todo 編集", cut.Markup);
    }

    [Fact]
    public void 既存Todoの値がフォームにプリセットされる()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _mockTodoApi.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var titleInput = cut.Find("#title");
        Assert.Equal("テストタスク", titleInput.GetAttribute("value"));
    }

    [Fact]
    public void 更新成功時にTodo詳細ページに遷移する()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _mockTodoApi.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);
        _mockTodoApi
            .Setup(x => x.UpdateAsync(1, It.IsAny<UpdateTodoRequest>()))
            .ReturnsAsync(todo);

        var navManager = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Act
        var form = cut.Find("form");
        form.Submit();

        // Assert
        Assert.EndsWith("/todos/1", navManager.Uri);
    }

    [Fact]
    public void 更新失敗時にエラーメッセージが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _mockTodoApi.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);
        _mockTodoApi
            .Setup(x => x.UpdateAsync(1, It.IsAny<UpdateTodoRequest>()))
            .ReturnsAsync((TodoResponse?)null);

        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Act
        var form = cut.Find("form");
        form.Submit();

        // Assert
        Assert.Contains("更新に失敗しました", cut.Markup);
    }

    [Fact]
    public void Todoが見つからない場合にメッセージが表示される()
    {
        // Arrange
        _mockTodoApi.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((TodoResponse?)null);

        // Act
        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 999));

        // Assert
        Assert.Contains("Todo が見つかりません", cut.Markup);
    }

    [Fact]
    public void 一覧に戻るリンクが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _mockTodoApi.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var backLink = cut.Find("a[href='/todos']");
        Assert.NotNull(backLink);
    }

    [Fact]
    public void 送信ボタンのテキストが更新になっている()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _mockTodoApi.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoEdit>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var submitButton = cut.Find("button[type='submit']");
        Assert.Equal("更新", submitButton.TextContent);
    }

    private static TodoResponse CreateSampleTodo()
    {
        return new TodoResponse
        {
            Id = 1,
            Title = "テストタスク",
            Description = "テストの説明",
            Status = TodoStatus.NotStarted,
            Priority = Priority.Medium,
            DueDate = new DateTime(2026, 12, 31),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = 1,
            AssignedToUserId = 2
        };
    }
}
