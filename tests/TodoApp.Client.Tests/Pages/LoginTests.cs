using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using TodoApp.Client.Pages;
using TodoApp.Client.Services;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Pages;

public class LoginTests : TestContext
{
    private readonly Mock<IAuthApiClient> _mockAuthApiClient;
    private readonly FakeAuthStateProvider _fakeAuthStateProvider;

    public LoginTests()
    {
        _mockAuthApiClient = new Mock<IAuthApiClient>();
        _fakeAuthStateProvider = new FakeAuthStateProvider();

        Services.AddSingleton(_mockAuthApiClient.Object);
        Services.AddSingleton<AuthStateProvider>(_fakeAuthStateProvider);
        Services.AddSingleton<AuthenticationStateProvider>(_fakeAuthStateProvider);
    }

    [Fact]
    public void Login_ページが正しくレンダリングされること()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert - メールアドレス入力フィールドが存在する
        var emailInput = cut.Find("input[type='email']");
        Assert.NotNull(emailInput);

        // パスワード入力フィールドが存在する
        var passwordInput = cut.Find("input[type='password']");
        Assert.NotNull(passwordInput);

        // ログインボタンが存在する
        var submitButton = cut.Find("button[type='submit']");
        Assert.NotNull(submitButton);
        Assert.Contains("ログイン", submitButton.TextContent);

        // ユーザー登録リンクが存在する
        var registerLink = cut.Find("a[href='register']");
        Assert.NotNull(registerLink);
    }

    [Fact]
    public async Task ログイン成功時にTodo一覧に遷移すること()
    {
        // Arrange
        var authResponse = new AuthResponse
        {
            UserId = 1,
            Email = "test@example.com",
            DisplayName = "テストユーザー",
            Token = "dummy-jwt-token"
        };

        _mockAuthApiClient
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        var navMan = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<Login>();

        // Act - フォームに値を入力
        cut.Find("input[type='email']").Change("test@example.com");
        cut.Find("input[type='password']").Change("password123");
        await cut.Find("form").SubmitAsync();

        // Assert
        _mockAuthApiClient.Verify(
            x => x.LoginAsync(It.Is<LoginRequest>(r =>
                r.Email == "test@example.com" && r.Password == "password123"), It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.True(_fakeAuthStateProvider.IsAuthenticated);
        Assert.Equal("dummy-jwt-token", _fakeAuthStateProvider.LastToken);
        Assert.EndsWith("/todos", navMan.Uri);
    }

    [Fact]
    public async Task ログイン失敗時にエラーメッセージを表示すること()
    {
        // Arrange
        _mockAuthApiClient
            .Setup(x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Unauthorized"));

        var cut = RenderComponent<Login>();

        // Act
        cut.Find("input[type='email']").Change("test@example.com");
        cut.Find("input[type='password']").Change("wrongpassword");
        await cut.Find("form").SubmitAsync();

        // Assert - エラーメッセージが表示される
        var errorMessage = cut.Find("[role='alert']");
        Assert.NotNull(errorMessage);
        Assert.NotEmpty(errorMessage.TextContent);
    }

    [Fact]
    public async Task バリデーションエラー時にAPIが呼ばれないこと()
    {
        // Arrange
        var cut = RenderComponent<Login>();

        // Act - 空のまま送信
        await cut.Find("form").SubmitAsync();

        // Assert - API は呼ばれない
        _mockAuthApiClient.Verify(
            x => x.LoginAsync(It.IsAny<LoginRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// テスト用の AuthStateProvider フェイク実装
    /// </summary>
    private class FakeAuthStateProvider : AuthStateProvider
    {
        public bool IsAuthenticated { get; private set; }
        public string? LastToken { get; private set; }

        public FakeAuthStateProvider()
            : base(Mock.Of<IJSRuntime>(), new HttpClient())
        {
        }

        public override Task MarkUserAsAuthenticatedAsync(string token)
        {
            IsAuthenticated = true;
            LastToken = token;
            return Task.CompletedTask;
        }
    }
}
