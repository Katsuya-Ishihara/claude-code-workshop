using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public interface ITodoService
{
    Task<TodoResponse> CreateAsync(CreateTodoRequest request, int createdByUserId, CancellationToken cancellationToken = default);
    Task<TodoResponse> UpdateAssigneeAsync(int id, UpdateTodoAssigneeRequest request, CancellationToken cancellationToken = default);
}
