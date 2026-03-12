using Bunit;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Client.Components;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Components;

public class UserSelectTests : TestContext
{
    private readonly Mock_UserApiClient _mockUserApiClient = new();

    public UserSelectTests()
    {
        Services.AddSingleton<IUserApiClient>(_mockUserApiClient);
    }

    [Fact]
    public void ユーザー一覧がドロップダウンに表示される()
    {
        // Arrange
        var users = new List<UserResponse>
        {
            new(1, "user1@example.com", "ユーザー1", UserRole.Member),
            new(2, "user2@example.com", "ユーザー2", UserRole.Admin),
        };
        _mockUserApiClient.UsersToReturn = users;

        // Act
        var cut = RenderComponent<UserSelect>();
        cut.WaitForState(() => cut.FindAll("option").Count > 1);

        // Assert
        var options = cut.FindAll("option");
        Assert.Equal(3, options.Count); // 未割当 + 2ユーザー
        Assert.Equal("ユーザー1", options[1].TextContent);
        Assert.Equal("1", options[1].GetAttribute("value"));
        Assert.Equal("ユーザー2", options[2].TextContent);
        Assert.Equal("2", options[2].GetAttribute("value"));
    }

    [Fact]
    public void 未割当オプションが含まれる()
    {
        // Arrange
        _mockUserApiClient.UsersToReturn = [];

        // Act
        var cut = RenderComponent<UserSelect>();
        cut.WaitForState(() => cut.FindAll("option").Count >= 1);

        // Assert
        var options = cut.FindAll("option");
        Assert.Single(options);
        Assert.Equal("", options[0].GetAttribute("value"));
        Assert.Contains("未割当", options[0].TextContent);
    }

    [Fact]
    public void 選択変更時にEventCallbackが呼ばれる()
    {
        // Arrange
        var users = new List<UserResponse>
        {
            new(1, "user1@example.com", "ユーザー1", UserRole.Member),
        };
        _mockUserApiClient.UsersToReturn = users;

        int? selectedUserId = null;
        var cut = RenderComponent<UserSelect>(parameters => parameters
            .Add(p => p.SelectedUserIdChanged, value => selectedUserId = value));
        cut.WaitForState(() => cut.FindAll("option").Count > 1);

        // Act
        cut.Find("select").Change("1");

        // Assert
        Assert.Equal(1, selectedUserId);
    }

    [Fact]
    public void 選択変更で未割当を選ぶとnullが返る()
    {
        // Arrange
        var users = new List<UserResponse>
        {
            new(1, "user1@example.com", "ユーザー1", UserRole.Member),
        };
        _mockUserApiClient.UsersToReturn = users;

        int? selectedUserId = 1;
        var cut = RenderComponent<UserSelect>(parameters => parameters
            .Add(p => p.SelectedUserId, 1)
            .Add(p => p.SelectedUserIdChanged, value => selectedUserId = value));
        cut.WaitForState(() => cut.FindAll("option").Count > 1);

        // Act
        cut.Find("select").Change("");

        // Assert
        Assert.Null(selectedUserId);
    }

    [Fact]
    public void ローディング中はdisabledになる()
    {
        // Arrange - API呼び出しが完了しないようにする
        _mockUserApiClient.DelayMs = 10000;
        _mockUserApiClient.UsersToReturn = [new(1, "user1@example.com", "ユーザー1", UserRole.Member)];

        // Act
        var cut = RenderComponent<UserSelect>();

        // Assert
        var select = cut.Find("select");
        Assert.True(select.HasAttribute("disabled"));
    }

    [Fact]
    public void カスタムラベルが表示される()
    {
        // Arrange
        _mockUserApiClient.UsersToReturn = [];

        // Act
        var cut = RenderComponent<UserSelect>(parameters => parameters
            .Add(p => p.Label, "レビュアー"));
        cut.WaitForState(() => cut.FindAll("option").Count >= 1);

        // Assert
        var label = cut.Find("label");
        Assert.Equal("レビュアー", label.TextContent);
    }

    [Fact]
    public void デフォルトラベルは担当者()
    {
        // Arrange
        _mockUserApiClient.UsersToReturn = [];

        // Act
        var cut = RenderComponent<UserSelect>();
        cut.WaitForState(() => cut.FindAll("option").Count >= 1);

        // Assert
        var label = cut.Find("label");
        Assert.Equal("担当者", label.TextContent);
    }

    [Fact]
    public void SelectedUserIdで初期選択値が設定される()
    {
        // Arrange
        var users = new List<UserResponse>
        {
            new(1, "user1@example.com", "ユーザー1", UserRole.Member),
            new(2, "user2@example.com", "ユーザー2", UserRole.Admin),
        };
        _mockUserApiClient.UsersToReturn = users;

        // Act
        var cut = RenderComponent<UserSelect>(parameters => parameters
            .Add(p => p.SelectedUserId, 2));
        cut.WaitForState(() => cut.FindAll("option").Count > 1);

        // Assert
        var select = cut.Find("select");
        Assert.Equal("2", select.GetAttribute("value"));
    }

    /// <summary>
    /// IUserApiClient のテスト用モック実装
    /// </summary>
    private class Mock_UserApiClient : IUserApiClient
    {
        public List<UserResponse> UsersToReturn { get; set; } = [];
        public int DelayMs { get; set; } = 0;

        public async Task<List<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            if (DelayMs > 0)
                await Task.Delay(DelayMs, cancellationToken);
            return UsersToReturn;
        }
    }
}
