using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoApp.Client.Pages;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Pages;

public class TodoDetailTests : TestContext
{
    private readonly Mock<ITodoApiClient> _todoApiClientMock;
    private readonly Mock<IUserApiClient> _userApiClientMock;
    private readonly ToastService _toastService;

    public TodoDetailTests()
    {
        _todoApiClientMock = new Mock<ITodoApiClient>();
        _userApiClientMock = new Mock<IUserApiClient>();
        _toastService = new ToastService();

        Services.AddSingleton(_todoApiClientMock.Object);
        Services.AddSingleton(_userApiClientMock.Object);
        Services.AddSingleton(_toastService);

        _userApiClientMock.Setup(x => x.GetUsersAsync())
            .ReturnsAsync(new List<UserResponse>
            {
                new(1, "user1@example.com", "ユーザー1", UserRole.Member),
                new(2, "user2@example.com", "ユーザー2", UserRole.Member)
            });
    }

    private TodoResponse CreateSampleTodo(
        int id = 1,
        string title = "テストTodo",
        string? description = "テスト説明文",
        TodoStatus status = TodoStatus.NotStarted,
        Priority priority = Priority.Medium,
        int? assignedToUserId = 1)
    {
        return new TodoResponse
        {
            Id = id,
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            ProgressRate = 0,
            DueDate = new DateTime(2026, 4, 1),
            CreatedAt = new DateTime(2026, 3, 1),
            UpdatedAt = new DateTime(2026, 3, 1),
            CreatedByUserId = 1,
            AssignedToUserId = assignedToUserId
        };
    }

    [Fact]
    public void Todo詳細情報が正しく表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var markup = cut.Markup;
        Assert.Contains("テストTodo", markup);
        Assert.Contains("テスト説明文", markup);
        Assert.Contains("未着手", markup);
    }

    [Fact]
    public void 優先度アイコンが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo(priority: Priority.High);
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        Assert.Contains("text-danger", cut.Markup);
    }

    [Fact]
    public void 期限日が表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        Assert.Contains("2026/04/01", cut.Markup);
    }

    [Fact]
    public void 担当者名が表示される()
    {
        // Arrange
        var todo = CreateSampleTodo(assignedToUserId: 1);
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        Assert.Contains("ユーザー1", cut.Markup);
    }

    [Fact]
    public async Task ステータス変更ボタンをクリックするとステータスが更新される()
    {
        // Arrange
        var todo = CreateSampleTodo(status: TodoStatus.NotStarted);
        var updatedTodo = CreateSampleTodo(status: TodoStatus.InProgress);
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);
        _todoApiClientMock.Setup(x => x.UpdateStatusAsync(1, It.Is<UpdateTodoStatusRequest>(r => r.Status == TodoStatus.InProgress)))
            .ReturnsAsync(updatedTodo);

        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Act
        var statusButton = cut.Find(".btn-status-change");
        await statusButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _todoApiClientMock.Verify(x => x.UpdateStatusAsync(1, It.Is<UpdateTodoStatusRequest>(r => r.Status == TodoStatus.InProgress)), Times.Once);
    }

    [Fact]
    public async Task InProgressからCompletedへステータス遷移する()
    {
        // Arrange
        var todo = CreateSampleTodo(status: TodoStatus.InProgress);
        var updatedTodo = CreateSampleTodo(status: TodoStatus.Completed);
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);
        _todoApiClientMock.Setup(x => x.UpdateStatusAsync(1, It.Is<UpdateTodoStatusRequest>(r => r.Status == TodoStatus.Completed)))
            .ReturnsAsync(updatedTodo);

        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Act
        var statusButton = cut.Find(".btn-status-change");
        await statusButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _todoApiClientMock.Verify(x => x.UpdateStatusAsync(1, It.Is<UpdateTodoStatusRequest>(r => r.Status == TodoStatus.Completed)), Times.Once);
    }

    [Fact]
    public void Completedの場合ステータス変更ボタンが非表示()
    {
        // Arrange
        var todo = CreateSampleTodo(status: TodoStatus.Completed);
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var statusButtons = cut.FindAll(".btn-status-change");
        Assert.Empty(statusButtons);
    }

    [Fact]
    public void 削除ボタンをクリックすると確認ダイアログが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Act
        var deleteButton = cut.Find(".btn-delete");
        deleteButton.Click();

        // Assert
        Assert.Contains("confirm-dialog-overlay", cut.Markup);
    }

    [Fact]
    public async Task 削除確認後にTodoが削除される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);
        _todoApiClientMock.Setup(x => x.DeleteAsync(1)).ReturnsAsync(true);

        var navManager = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();

        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Show dialog
        var deleteButton = cut.Find(".btn-delete");
        deleteButton.Click();

        // Act - confirm deletion
        var confirmButton = cut.Find(".btn-confirm");
        await confirmButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert
        _todoApiClientMock.Verify(x => x.DeleteAsync(1), Times.Once);
        Assert.EndsWith("/todos", navManager.Uri);
    }

    [Fact]
    public void 削除キャンセル後にダイアログが閉じる()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Show dialog
        var deleteButton = cut.Find(".btn-delete");
        deleteButton.Click();

        // Act - cancel
        var cancelButton = cut.Find(".btn-cancel");
        cancelButton.Click();

        // Assert
        Assert.DoesNotContain("confirm-dialog-overlay", cut.Markup);
    }

    [Fact]
    public void Todoが見つからない場合は404メッセージが表示される()
    {
        // Arrange
        _todoApiClientMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((TodoResponse?)null);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 999));

        // Assert
        Assert.Contains("見つかりませんでした", cut.Markup);
    }

    [Fact]
    public void ローディング中はスピナーが表示される()
    {
        // Arrange - don't complete the task so it stays loading
        var tcs = new TaskCompletionSource<TodoResponse?>();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).Returns(tcs.Task);

        // Also need to handle users loading
        var usersTcs = new TaskCompletionSource<List<UserResponse>>();
        _userApiClientMock.Setup(x => x.GetUsersAsync()).Returns(usersTcs.Task);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        Assert.Contains("loading", cut.Markup.ToLower());
    }

    [Fact]
    public void 一覧に戻るリンクが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var backLink = cut.Find("a[href='/todos']");
        Assert.NotNull(backLink);
    }

    [Fact]
    public void 編集ボタンが表示される()
    {
        // Arrange
        var todo = CreateSampleTodo();
        _todoApiClientMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(todo);

        // Act
        var cut = RenderComponent<TodoDetail>(parameters =>
            parameters.Add(p => p.Id, 1));

        // Assert
        var editLink = cut.Find("a[href='/todos/1/edit']");
        Assert.NotNull(editLink);
    }
}
