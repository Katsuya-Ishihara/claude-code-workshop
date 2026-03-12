using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public interface ITodoService
{
    Task<PaginatedResponse<TodoResponse>> GetAllAsync(GetTodosRequest request, CancellationToken cancellationToken = default);
    Task<TodoResponse> CreateAsync(CreateTodoRequest request, int createdByUserId, CancellationToken cancellationToken = default);
    Task<TodoResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TodoResponse> UpdateAsync(int id, UpdateTodoRequest request, int userId, CancellationToken cancellationToken = default);
    Task<TodoResponse> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default);
    Task<TodoResponse> UpdateAssigneeAsync(int id, UpdateTodoAssigneeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);
}
