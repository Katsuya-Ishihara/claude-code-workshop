using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TodoApp.Client.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Tests.Services;

public class UserApiClientTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task GetUsersAsync_ReturnsUserList()
    {
        // Arrange
        var expectedUsers = new List<UserResponse>
        {
            new(1, "alice@example.com", "Alice", UserRole.Admin),
            new(2, "bob@example.com", "Bob", UserRole.Member)
        };

        var handler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(expectedUsers, JsonOptions));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new UserApiClient(httpClient);

        // Act
        var result = await client.GetUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].DisplayName);
        Assert.Equal("alice@example.com", result[0].Email);
        Assert.Equal(UserRole.Admin, result[0].Role);
        Assert.Equal("Bob", result[1].DisplayName);
        Assert.Equal(UserRole.Member, result[1].Role);
    }

    [Fact]
    public async Task GetUsersAsync_SendsGetRequestToCorrectEndpoint()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<UserResponse>(), JsonOptions));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new UserApiClient(httpClient);

        // Act
        await client.GetUsersAsync();

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("http://localhost/api/users", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetUsersAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(new List<UserResponse>(), JsonOptions));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new UserApiClient(httpClient);

        // Act
        var result = await client.GetUsersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUsersAsync_HttpError_ThrowsHttpRequestException()
    {
        // Arrange
        var handler = new MockHttpMessageHandler(
            HttpStatusCode.InternalServerError,
            "Internal Server Error");

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };

        var client = new UserApiClient(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetUsersAsync());
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseContent;

        public HttpRequestMessage? LastRequest { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
        {
            _statusCode = statusCode;
            _responseContent = responseContent;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseContent, System.Text.Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}
