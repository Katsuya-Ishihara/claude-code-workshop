using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using TodoApp.Client.Services;

namespace TodoApp.Client.Tests.Services;

public class AuthStateProviderTests
{
    private readonly Mock<IJSRuntime> _mockJsRuntime;
    private readonly HttpClient _httpClient;
    private readonly AuthStateProvider _sut;

    public AuthStateProviderTests()
    {
        _mockJsRuntime = new Mock<IJSRuntime>();
        _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost/") };
        _sut = new AuthStateProvider(_mockJsRuntime.Object, _httpClient);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_トークンなしの場合は匿名ユーザーを返す()
    {
        // Arrange
        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")))
            .ReturnsAsync((string)null!);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_有効なJWTトークンがある場合は認証済みユーザーを返す()
    {
        // Arrange
        // JWT token with payload: {"sub":"1","email":"test@example.com","name":"Test User","role":"Member","exp":9999999999}
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmFtZSI6IlRlc3QgVXNlciIsInJvbGUiOiJNZW1iZXIiLCJleHAiOjk5OTk5OTk5OTl9.placeholder";

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")))
            .ReturnsAsync(token);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        Assert.True(state.User.Identity?.IsAuthenticated);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal(token, _httpClient.DefaultRequestHeaders.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetAuthenticationStateAsync_期限切れJWTトークンの場合はログアウトして匿名ユーザーを返す()
    {
        // Arrange
        // JWT token with payload: {"sub":"1","email":"test@example.com","name":"Test User","role":"Member","exp":1000000000} (expired)
        var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJ0ZXN0QGV4YW1wbGUuY29tIiwibmFtZSI6IlRlc3QgVXNlciIsInJvbGUiOiJNZW1iZXIiLCJleHAiOjEwMDAwMDAwMDB9.placeholder";

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<string>(
                "localStorage.getItem",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")))
            .ReturnsAsync(token);

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.removeItem",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")))
            .ReturnsAsync((IJSVoidResult)null!);

        // Act
        var state = await _sut.GetAuthenticationStateAsync();

        // Assert
        Assert.False(state.User.Identity?.IsAuthenticated);
        _mockJsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
            "localStorage.removeItem",
            It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")),
            Times.Once);
    }

    [Fact]
    public async Task MarkUserAsAuthenticated_トークンを保存し認証状態を通知する()
    {
        // Arrange
        var token = "test-jwt-token";
        var authStateChanged = false;
        _sut.AuthenticationStateChanged += _ => authStateChanged = true;

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.setItem",
                It.Is<object[]>(args => args.Length == 2 && (string)args[0] == "authToken" && (string)args[1] == token)))
            .ReturnsAsync((IJSVoidResult)null!);

        // Act
        await _sut.MarkUserAsAuthenticatedAsync(token);

        // Assert
        Assert.True(authStateChanged);
        Assert.Equal("Bearer", _httpClient.DefaultRequestHeaders.Authorization?.Scheme);
        Assert.Equal(token, _httpClient.DefaultRequestHeaders.Authorization?.Parameter);
        _mockJsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
            "localStorage.setItem",
            It.Is<object[]>(args => args.Length == 2 && (string)args[0] == "authToken" && (string)args[1] == token)),
            Times.Once);
    }

    [Fact]
    public async Task MarkUserAsLoggedOut_トークンを削除し匿名状態に戻す()
    {
        // Arrange
        var authStateChanged = false;
        _sut.AuthenticationStateChanged += _ => authStateChanged = true;

        _mockJsRuntime
            .Setup(js => js.InvokeAsync<IJSVoidResult>(
                "localStorage.removeItem",
                It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")))
            .ReturnsAsync((IJSVoidResult)null!);

        // Act
        await _sut.MarkUserAsLoggedOutAsync();

        // Assert
        Assert.True(authStateChanged);
        Assert.Null(_httpClient.DefaultRequestHeaders.Authorization);
        _mockJsRuntime.Verify(js => js.InvokeAsync<IJSVoidResult>(
            "localStorage.removeItem",
            It.Is<object[]>(args => args.Length == 1 && (string)args[0] == "authToken")),
            Times.Once);
    }
}
