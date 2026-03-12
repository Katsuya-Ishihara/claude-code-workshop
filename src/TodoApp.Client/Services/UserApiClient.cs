using System.Net.Http.Json;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Services;

/// <summary>
/// ユーザーAPIを呼び出すHTTPクライアント
/// </summary>
public class UserApiClient : IUserApiClient
{
    private readonly HttpClient _httpClient;

    public UserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<List<UserResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/users", cancellationToken);
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<UserResponse>>(cancellationToken: cancellationToken);
        return users ?? [];
    }
}
