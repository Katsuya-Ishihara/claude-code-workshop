using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Client.Components;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Components;

public class TodoFormTests : TestContext
{
    private readonly MockUserApiClient _mockUserApiClient;

    public TodoFormTests()
    {
        _mockUserApiClient = new MockUserApiClient();
        Services.AddSingleton<IUserApiClient>(_mockUserApiClient);
    }

    [Fact]
    public void TodoForm_レンダリング時にタイトル入力欄が表示される()
    {
        var cut = RenderComponent<TodoForm>();

        var titleInput = cut.Find("input[id='title']");
        Assert.NotNull(titleInput);
    }

    [Fact]
    public void TodoForm_レンダリング時に説明テキストエリアが表示される()
    {
        var cut = RenderComponent<TodoForm>();

        var descriptionTextarea = cut.Find("textarea[id='description']");
        Assert.NotNull(descriptionTextarea);
    }

    [Fact]
    public void TodoForm_レンダリング時に優先度セレクトが表示される()
    {
        var cut = RenderComponent<TodoForm>();

        var prioritySelect = cut.Find("select[id='priority']");
        Assert.NotNull(prioritySelect);
    }

    [Fact]
    public void TodoForm_レンダリング時に期限入力欄が表示される()
    {
        var cut = RenderComponent<TodoForm>();

        var dueDateInput = cut.Find("input[id='dueDate']");
        Assert.NotNull(dueDateInput);
    }

    [Fact]
    public void TodoForm_レンダリング時にUserSelectコンポーネントが表示される()
    {
        var cut = RenderComponent<TodoForm>();

        // UserSelect renders a label with "担当者"
        var markup = cut.Markup;
        Assert.Contains("担当者", markup);
    }

    [Fact]
    public void TodoForm_デフォルトのボタンテキストは作成()
    {
        var cut = RenderComponent<TodoForm>();

        var button = cut.Find("button[type='submit']");
        Assert.Equal("作成", button.TextContent.Trim());
    }

    [Fact]
    public void TodoForm_SubmitButtonTextパラメータでボタンテキストを変更できる()
    {
        var cut = RenderComponent<TodoForm>(parameters => parameters
            .Add(p => p.SubmitButtonText, "更新"));

        var button = cut.Find("button[type='submit']");
        Assert.Equal("更新", button.TextContent.Trim());
    }

    [Fact]
    public void TodoForm_InitialValueが指定された場合にフォームに初期値が設定される()
    {
        var initialValue = new CreateTodoRequest
        {
            Title = "テストタイトル",
            Description = "テスト説明",
            Priority = Priority.High,
            DueDate = new DateTime(2026, 12, 31),
            AssignedToUserId = 1
        };

        var cut = RenderComponent<TodoForm>(parameters => parameters
            .Add(p => p.InitialValue, initialValue));

        var titleInput = cut.Find("input[id='title']");
        Assert.Equal("テストタイトル", titleInput.GetAttribute("value"));

        var descriptionTextarea = cut.Find("textarea[id='description']");
        // InputTextArea renders value as inner text or via value attribute
        var descValue = descriptionTextarea.GetAttribute("value") ?? descriptionTextarea.TextContent;
        Assert.Equal("テスト説明", descValue);
    }

    [Fact]
    public void TodoForm_タイトル未入力で送信するとバリデーションエラーが表示される()
    {
        var cut = RenderComponent<TodoForm>();

        var form = cut.Find("form");
        form.Submit();

        var markup = cut.Markup;
        Assert.Contains("タイトルは必須です", markup);
    }

    [Fact]
    public void TodoForm_有効な入力で送信するとOnSubmitが呼ばれる()
    {
        CreateTodoRequest? submittedRequest = null;
        var cut = RenderComponent<TodoForm>(parameters => parameters
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<CreateTodoRequest>(this, req =>
            {
                submittedRequest = req;
            })));

        cut.Find("input[id='title']").Change("新しいタスク");
        cut.Find("form").Submit();

        Assert.NotNull(submittedRequest);
        Assert.Equal("新しいタスク", submittedRequest!.Title);
    }

    [Fact]
    public void TodoForm_優先度セレクトにLow_Medium_Highの選択肢がある()
    {
        var cut = RenderComponent<TodoForm>();

        var prioritySelect = cut.Find("select[id='priority']");
        var options = prioritySelect.QuerySelectorAll("option");

        // 未選択 + Low + Medium + High = 4
        Assert.Equal(4, options.Length);
    }

    private class MockUserApiClient : IUserApiClient
    {
        public Task<List<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<UserResponse>
            {
                new(1, "user1@example.com", "ユーザー1", UserRole.Member),
                new(2, "user2@example.com", "ユーザー2", UserRole.Member)
            });
        }
    }
}
