using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TodoApp.Client.Pages;
using TodoApp.Client.Services;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Pages;

public class RegisterPageTest : TestContext
{
    private readonly Mock<IAuthApiClient> _mockAuthApiClient;

    public RegisterPageTest()
    {
        _mockAuthApiClient = new Mock<IAuthApiClient>();
        Services.AddSingleton(_mockAuthApiClient.Object);
    }

    [Fact]
    public void Register_ページが正しくレンダリングされる()
    {
        var cut = RenderComponent<Register>();

        Assert.NotNull(cut.Find("input[type='email']"));
        Assert.NotNull(cut.Find("input[type='password']"));
        Assert.NotNull(cut.Find("input[id='displayName']"));
        Assert.NotNull(cut.Find("button[type='submit']"));
    }

    [Fact]
    public void Register_ログイン画面へのリンクが表示される()
    {
        var cut = RenderComponent<Register>();

        var link = cut.Find("a[href='login']");
        Assert.NotNull(link);
    }

    [Fact]
    public async Task Register_正常な入力で登録成功時にログイン画面へ遷移する()
    {
        _mockAuthApiClient
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(new AuthResponse
            {
                UserId = 1,
                Email = "test@example.com",
                DisplayName = "テストユーザー",
                Token = "dummy-token"
            });

        var navManager = Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>();
        var cut = RenderComponent<Register>();

        cut.Find("input[type='email']").Change("test@example.com");
        cut.Find("input[type='password']").Change("password123");
        cut.Find("input[id='displayName']").Change("テストユーザー");

        await cut.Find("form").SubmitAsync();

        _mockAuthApiClient.Verify(
            x => x.RegisterAsync(It.Is<RegisterRequest>(r =>
                r.Email == "test@example.com" &&
                r.Password == "password123" &&
                r.DisplayName == "テストユーザー")),
            Times.Once);

        Assert.EndsWith("/login", navManager.Uri);
    }

    [Fact]
    public async Task Register_API失敗時にエラーメッセージが表示される()
    {
        _mockAuthApiClient
            .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new HttpRequestException("このメールアドレスは既に登録されています"));

        var cut = RenderComponent<Register>();

        cut.Find("input[type='email']").Change("test@example.com");
        cut.Find("input[type='password']").Change("password123");
        cut.Find("input[id='displayName']").Change("テストユーザー");

        await cut.Find("form").SubmitAsync();

        var errorMessage = cut.Find("[class*='error']");
        Assert.NotNull(errorMessage);
        Assert.Contains("メールアドレスは既に登録されています", errorMessage.TextContent);
    }

    [Fact]
    public async Task Register_バリデーションエラー時にAPIが呼ばれない()
    {
        var cut = RenderComponent<Register>();

        // Submit empty form
        await cut.Find("form").SubmitAsync();

        _mockAuthApiClient.Verify(
            x => x.RegisterAsync(It.IsAny<RegisterRequest>()),
            Times.Never);
    }

    [Fact]
    public void Register_ページタイトルが表示される()
    {
        var cut = RenderComponent<Register>();

        var heading = cut.Find("h2");
        Assert.Contains("ユーザー登録", heading.TextContent);
    }
}
