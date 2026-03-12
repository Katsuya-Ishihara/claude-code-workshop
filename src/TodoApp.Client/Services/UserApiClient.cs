using System.Net.Http.Json;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Services;

/// <summary>
/// ユーザーAPI呼び出しHTTPクライアント
/// </summary>
public class UserApiClient : IUserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<List<UserResponse>> GetUsersAsync()
    {
        var response = await _httpClient.GetAsync("api/users");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<UserResponse>>() ?? new List<UserResponse>();
    }
}
