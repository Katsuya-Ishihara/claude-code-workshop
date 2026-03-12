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
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <inheritdoc />
    public async Task<List<TodoResponse>> GetAllAsync()
    {
        var response = await _httpClient.GetAsync("api/todos");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<TodoResponse>>() ?? new List<TodoResponse>();
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> GetByIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"api/todos/{id}");

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TodoResponse>();
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> CreateAsync(CreateTodoRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("api/todos", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>();
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> UpdateAsync(int id, UpdateTodoRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/todos/{id}", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>();
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"api/todos/{id}");
        return response.StatusCode == HttpStatusCode.NoContent;
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> UpdateStatusAsync(int id, UpdateTodoStatusRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/todos/{id}/status", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>();
    }

    /// <inheritdoc />
    public async Task<TodoResponse?> UpdateAssigneeAsync(int id, UpdateTodoAssigneeRequest request)
    {
        var response = await _httpClient.PatchAsJsonAsync($"api/todos/{id}/assign", request);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<TodoResponse>();
    }
}
