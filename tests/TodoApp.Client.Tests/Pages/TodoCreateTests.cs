using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoApp.Client.Pages;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Pages;

public class TodoCreateTests : TestContext
{
    private readonly Mock<ITodoApiClient> _mockTodoApi;
    private readonly Mock<IUserApiClient> _mockUserApi;

    public TodoCreateTests()
    {
        _mockTodoApi = new Mock<ITodoApiClient>();
        _mockUserApi = new Mock<IUserApiClient>();

        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserResponse>
            {
                new(1, "user1@example.com", "User 1", UserRole.Member),
                new(2, "user2@example.com", "User 2", UserRole.Member)
            });

        Services.AddSingleton(_mockTodoApi.Object);
        Services.AddSingleton(_mockUserApi.Object);
    }

    [Fact]
    public void ページが正しくレンダリングされ_TodoFormが表示される()
    {
        // Act
        var cut = RenderComponent<TodoCreate>();

        // Assert
        Assert.Contains("Todo 作成", cut.Markup);
        cut.FindComponent<TodoApp.Client.Components.TodoForm>();
        Assert.Contains("作成", cut.Markup);
    }

    [Fact]
    public void 作成成功時にTodos一覧ページに遷移する()
    {
        // Arrange
        var expectedResponse = new TodoResponse
        {
            Id = 1,
            Title = "テストタスク",
            Status = TodoStatus.NotStarted,
            Priority = Priority.Medium,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = 1
        };

        _mockTodoApi.Setup(x => x.CreateAsync(It.IsAny<CreateTodoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var navManager = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var cut = RenderComponent<TodoCreate>();

        // Act - Fill in the form and submit
        var titleInput = cut.Find("#title");
        titleInput.Change("テストタスク");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        _mockTodoApi.Verify(x => x.CreateAsync(It.Is<CreateTodoRequest>(r => r.Title == "テストタスク"), It.IsAny<CancellationToken>()), Times.Once);
        Assert.EndsWith("/todos", navManager.Uri);
    }

    [Fact]
    public void 作成失敗時にエラーメッセージが表示される()
    {
        // Arrange
        _mockTodoApi.Setup(x => x.CreateAsync(It.IsAny<CreateTodoRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TodoResponse?)null);

        var cut = RenderComponent<TodoCreate>();

        // Act - Fill in the form and submit
        var titleInput = cut.Find("#title");
        titleInput.Change("テストタスク");

        var form = cut.Find("form");
        form.Submit();

        // Assert
        _mockTodoApi.Verify(x => x.CreateAsync(It.IsAny<CreateTodoRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("Todo の作成に失敗しました", cut.Markup);
    }
}
