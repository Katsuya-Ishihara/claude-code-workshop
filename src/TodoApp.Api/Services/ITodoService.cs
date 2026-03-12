using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public interface ITodoService
{
    Task<TodoResponse> CreateAsync(CreateTodoRequest request, int createdByUserId, CancellationToken cancellationToken = default);
    Task<TodoResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TodoResponse> UpdateStatusAsync(int id, UpdateTodoStatusRequest request, CancellationToken cancellationToken = default);
}
