using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoApp.Client.Pages;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Pages;

public class TodoListTests : TestContext
{
    private readonly Mock<ITodoApiClient> _mockTodoApi;
    private readonly Mock<IUserApiClient> _mockUserApi;

    public TodoListTests()
    {
        _mockTodoApi = new Mock<ITodoApiClient>();
        _mockUserApi = new Mock<IUserApiClient>();

        Services.AddScoped(_ => _mockTodoApi.Object);
        Services.AddScoped(_ => _mockUserApi.Object);
        Services.AddScoped<ToastService>();
    }

    private static PaginatedResponse<TodoResponse> CreatePagedResponse(List<TodoResponse> items, int page = 1, int pageSize = 10, int? totalCount = null)
    {
        var total = totalCount ?? items.Count;
        return new PaginatedResponse<TodoResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    private List<TodoResponse> CreateSampleTodos(int count = 3)
    {
        var todos = new List<TodoResponse>();
        for (int i = 1; i <= count; i++)
        {
            todos.Add(new TodoResponse
            {
                Id = i,
                Title = $"Todo {i}",
                Description = $"Description {i}",
                Status = TodoStatus.NotStarted,
                Priority = Priority.Medium,
                DueDate = DateTime.Now.AddDays(i),
                CreatedByUserId = 1,
                AssignedToUserId = i % 2 == 0 ? 1 : null
            });
        }
        return todos;
    }

    private List<UserResponse> CreateSampleUsers()
    {
        return new List<UserResponse>
        {
            new UserResponse(1, "user1@example.com", "User 1", UserRole.Member),
            new UserResponse(2, "user2@example.com", "User 2", UserRole.Member)
        };
    }

    [Fact]
    public void ローディング中はスピナーが表示される()
    {
        // Arrange - API が完了しないようにする
        var tcs = new TaskCompletionSource<PaginatedResponse<TodoResponse>>();
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(tcs.Task);
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).Returns(new TaskCompletionSource<List<UserResponse>>().Task);

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert
        Assert.Contains("読み込み中", cut.Markup);
    }

    [Fact]
    public void Todo一覧が正しく表示される()
    {
        // Arrange
        var todos = CreateSampleTodos();
        var users = CreateSampleUsers();
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(todos));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert
        Assert.Contains("Todo 1", cut.Markup);
        Assert.Contains("Todo 2", cut.Markup);
        Assert.Contains("Todo 3", cut.Markup);
    }

    [Fact]
    public void Todoが空の場合はメッセージが表示される()
    {
        // Arrange
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(new List<TodoResponse>()));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserResponse>());

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert
        Assert.Contains("Todoがありません", cut.Markup);
    }

    [Fact]
    public void 新規作成ボタンが表示される()
    {
        // Arrange
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(new List<TodoResponse>()));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserResponse>());

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert
        var createButton = cut.Find(".btn-create");
        Assert.NotNull(createButton);
        Assert.Contains("新規作成", createButton.TextContent);
    }

    [Fact]
    public void 担当者名が正しく表示される()
    {
        // Arrange
        var todos = new List<TodoResponse>
        {
            new TodoResponse
            {
                Id = 1, Title = "Assigned Todo", Status = TodoStatus.NotStarted,
                Priority = Priority.Medium, AssignedToUserId = 1, CreatedByUserId = 1
            },
            new TodoResponse
            {
                Id = 2, Title = "Unassigned Todo", Status = TodoStatus.NotStarted,
                Priority = Priority.Low, AssignedToUserId = null, CreatedByUserId = 1
            }
        };
        var users = new List<UserResponse>
        {
            new UserResponse(1, "user1@example.com", "Tanaka Taro", UserRole.Member)
        };
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(todos));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert - TodoCard 内に担当者名が表示されることを確認
        var cards = cut.FindAll(".todo-card-assignee");
        Assert.Equal(2, cards.Count);
        Assert.Contains("Tanaka Taro", cards[0].TextContent);
        Assert.Contains("未割り当て", cards[1].TextContent);
    }

    [Fact]
    public void 検索でTodoがフィルタリングされる()
    {
        // Arrange - 初期表示用
        var allTodos = new List<TodoResponse>
        {
            new TodoResponse { Id = 1, Title = "買い物リスト", Status = TodoStatus.NotStarted, Priority = Priority.Medium, CreatedByUserId = 1 },
            new TodoResponse { Id = 2, Title = "レポート作成", Status = TodoStatus.InProgress, Priority = Priority.High, CreatedByUserId = 1 },
            new TodoResponse { Id = 3, Title = "会議準備", Status = TodoStatus.NotStarted, Priority = Priority.Low, CreatedByUserId = 1 }
        };
        var filteredTodos = new List<TodoResponse>
        {
            new TodoResponse { Id = 2, Title = "レポート作成", Status = TodoStatus.InProgress, Priority = Priority.High, CreatedByUserId = 1 }
        };
        // 初期表示: キーワードなし
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(allTodos));
        // 検索時: キーワードあり
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), "レポート", It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(filteredTodos));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserResponse>());

        var cut = RenderComponent<TodoList>();

        // Act - SearchBar の検索ボタンをクリック
        var searchInput = cut.Find(".search-input");
        searchInput.Input("レポート");
        var searchButton = cut.Find(".search-button");
        searchButton.Click();

        // Assert
        Assert.Contains("レポート作成", cut.Markup);
        Assert.DoesNotContain("買い物リスト", cut.Markup);
        Assert.DoesNotContain("会議準備", cut.Markup);
    }

    [Fact]
    public void ページングが正しく動作する()
    {
        // Arrange - サーバーサイドページネーション（15件中1ページ目10件）
        var page1Todos = CreateSampleTodos(10);
        var page2Todos = new List<TodoResponse>();
        for (int i = 11; i <= 15; i++)
        {
            page2Todos.Add(new TodoResponse
            {
                Id = i, Title = $"Todo {i}", Description = $"Description {i}",
                Status = TodoStatus.NotStarted, Priority = Priority.Medium,
                DueDate = DateTime.Now.AddDays(i), CreatedByUserId = 1
            });
        }

        _mockTodoApi.Setup(x => x.GetAllPagedAsync(1, It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResponse(page1Todos, page: 1, pageSize: 10, totalCount: 15));
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(2, It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreatePagedResponse(page2Todos, page: 2, pageSize: 10, totalCount: 15));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserResponse>());

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert - 1ページ目は10件のカードが表示される
        var cards = cut.FindAll(".todo-card");
        Assert.Equal(10, cards.Count);
        Assert.Contains("Todo 1", cut.Markup);
        Assert.Contains("Todo 10", cut.Markup);

        // Act - 2ページ目に遷移
        var nextPage = cut.Find(".next-page button");
        nextPage.Click();

        // Assert - 2ページ目は残り5件のカードが表示される
        var cardsPage2 = cut.FindAll(".todo-card");
        Assert.Equal(5, cardsPage2.Count);
        Assert.Contains("Todo 11", cut.Markup);
        Assert.Contains("Todo 15", cut.Markup);
    }

    [Fact]
    public void API呼び出し失敗時にエラーメッセージが表示される()
    {
        // Arrange
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("API error"));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserResponse>());

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert
        Assert.Contains("データの読み込みに失敗しました", cut.Markup);
    }

    [Fact]
    public void ページタイトルが表示される()
    {
        // Arrange
        _mockTodoApi.Setup(x => x.GetAllPagedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ReturnsAsync(CreatePagedResponse(new List<TodoResponse>()));
        _mockUserApi.Setup(x => x.GetUsersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<UserResponse>());

        // Act
        var cut = RenderComponent<TodoList>();

        // Assert
        Assert.Contains("Todo 一覧", cut.Markup);
    }
}
