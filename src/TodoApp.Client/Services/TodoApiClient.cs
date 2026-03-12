using System.Net;
using System.Net.Http.Json;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Client.Services;

/// <summary>
/// Todo API を呼び出す HTTP クライアント。
/// </summary>
public class TodoApiClient : ITodoApiClient
{
    private readonly HttpClient _httpClient;

    public TodoApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public async Task<List<TodoResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/todos", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TodoResponse>>(cancellationToken: cancellationToken) ?? new List<TodoResponse>();
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/todos/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TodoResponse>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> CreateAsync(CreateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/todos", request, cancellationToken);

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException($"サーバーエラーが発生しました。ステータスコード: {(int)response.StatusCode}", null, response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> UpdateAsync(int id, UpdateTodoRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/todos/{id}", request, cancellationToken);

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException($"サーバーエラーが発生しました。ステータスコード: {(int)response.StatusCode}", null, response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/todos/{id}", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/todos/{id}/status", request, cancellationToken);

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException($"サーバーエラーが発生しました。ステータスコード: {(int)response.StatusCode}", null, response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> UpdateAssigneeAsync(int id, UpdateTodoAssigneeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/todos/{id}/assign", request, cancellationToken);

        if ((int)response.StatusCode >= 500)
        {
            throw new HttpRequestException($"サーバーエラーが発生しました。ステータスコード: {(int)response.StatusCode}", null, response.StatusCode);
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>(cancellationToken: cancellationToken);
    }
}
