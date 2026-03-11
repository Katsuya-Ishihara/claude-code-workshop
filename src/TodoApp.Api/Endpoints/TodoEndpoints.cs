using TodoApp.Api.Services;
using TodoApp.Shared.Models;
using TodoApp.Shared.Requests;

namespace TodoApp.Api.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this WebApplication app)
    {
        app.MapGet("/api/todos", async (
            ITodoService todoService,
            int? page,
            int? pageSize,
            TodoStatus? status,
            Priority? priority,
            int? assignedToUserId,
            string? keyword,
            string? sortBy,
            bool? sortDesc,
            CancellationToken cancellationToken) =>
        {
            var request = new GetTodosRequest
            {
                Page = page ?? 1,
                PageSize = pageSize ?? 10,
                Status = status,
                Priority = priority,
                AssignedToUserId = assignedToUserId,
                Keyword = keyword,
                SortBy = sortBy,
                SortDesc = sortDesc ?? false
            };

            var result = await todoService.GetAllAsync(request, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();
    }
}
