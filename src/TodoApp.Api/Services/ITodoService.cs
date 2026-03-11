using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public interface ITodoService
{
    Task<PaginatedResponse<TodoResponse>> GetAllAsync(GetTodosRequest request, CancellationToken cancellationToken = default);
}
