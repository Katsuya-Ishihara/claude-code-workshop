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
        var paged = await GetAllPagedAsync(page: 1, pageSize: 100, cancellationToken: cancellationToken);
        return paged.Items;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<TodoResponse>> GetAllPagedAsync(int page = 1, int pageSize = 10, string? keyword = null, CancellationToken cancellationToken = default)
    {
        var url = $"api/todos?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            url += $"&keyword={Uri.EscapeDataString(keyword)}";
        }
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaginatedResponse<TodoResponse>>(cancellationToken: cancellationToken)
            ?? new PaginatedResponse<TodoResponse>();
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
