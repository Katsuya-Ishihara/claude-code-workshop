using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RichardSzalay.MockHttp;
using TodoApp.Client.Services;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Services;

public class AuthApiClientTests
{
    private readonly MockHttpMessageHandler _mockHttp;
    private readonly HttpClient _httpClient;
    private readonly AuthApiClient _sut;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuthApiClientTests()
    {
        _mockHttp = new MockHttpMessageHandler();
        _httpClient = _mockHttp.ToHttpClient();
        _httpClient.BaseAddress = new Uri("http://localhost/");
        _sut = new AuthApiClient(_httpClient);
    }

    [Fact]
    public async Task RegisterAsync_正常なリクエストでAuthResponseを返す()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            DisplayName = "Test User"
        };
        var expectedResponse = new AuthResponse
        {
            UserId = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            Token = "jwt-token-123"
        };

        _mockHttp.When(HttpMethod.Post, "http://localhost/api/auth/register")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        Assert.Equal(expectedResponse.UserId, result.UserId);
        Assert.Equal(expectedResponse.Email, result.Email);
        Assert.Equal(expectedResponse.DisplayName, result.DisplayName);
        Assert.Equal(expectedResponse.Token, result.Token);
    }

    [Fact]
    public async Task RegisterAsync_サーバーエラー時にHttpRequestExceptionをスローする()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "password123",
            DisplayName = "Test User"
        };

        _mockHttp.When(HttpMethod.Post, "http://localhost/api/auth/register")
            .Respond(HttpStatusCode.InternalServerError);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_正常なリクエストでAuthResponseを返す()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "password123"
        };
        var expectedResponse = new AuthResponse
        {
            UserId = 1,
            Email = "test@example.com",
            DisplayName = "Test User",
            Token = "jwt-token-456"
        };

        _mockHttp.When(HttpMethod.Post, "http://localhost/api/auth/login")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        Assert.Equal(expectedResponse.UserId, result.UserId);
        Assert.Equal(expectedResponse.Email, result.Email);
        Assert.Equal(expectedResponse.DisplayName, result.DisplayName);
        Assert.Equal(expectedResponse.Token, result.Token);
    }

    [Fact]
    public async Task LoginAsync_認証失敗時にHttpRequestExceptionをスローする()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "wrong-password"
        };

        _mockHttp.When(HttpMethod.Post, "http://localhost/api/auth/login")
            .Respond(HttpStatusCode.Unauthorized);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.LoginAsync(request));
    }

    [Fact]
    public async Task GetMeAsync_正常なレスポンスでUserResponseを返す()
    {
        // Arrange
        var expectedResponse = new UserResponse(
            Id: 1,
            Email: "test@example.com",
            DisplayName: "Test User",
            Role: TodoApp.Shared.Models.UserRole.Member
        );

        _mockHttp.When(HttpMethod.Get, "http://localhost/api/auth/me")
            .Respond(HttpStatusCode.OK, JsonContent.Create(expectedResponse));

        // Act
        var result = await _sut.GetMeAsync();

        // Assert
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Email, result.Email);
        Assert.Equal(expectedResponse.DisplayName, result.DisplayName);
        Assert.Equal(expectedResponse.Role, result.Role);
    }

    [Fact]
    public async Task GetMeAsync_未認証時にHttpRequestExceptionをスローする()
    {
        // Arrange
        _mockHttp.When(HttpMethod.Get, "http://localhost/api/auth/me")
            .Respond(HttpStatusCode.Unauthorized);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _sut.GetMeAsync());
    }
}
