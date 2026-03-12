using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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

        app.MapPost("/api/todos", async (CreateTodoRequest request, ClaimsPrincipal user, ITodoService todoService, CancellationToken cancellationToken) =>
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, context, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? string.Empty).ToArray());
                return Results.ValidationProblem(errors);
            }

            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var response = await todoService.CreateAsync(request, userId, cancellationToken);
            return Results.Created($"/api/todos/{response.Id}", response);
        }).RequireAuthorization();

        app.MapGet("/api/todos/{id:int}", async (int id, ITodoService todoService, CancellationToken cancellationToken) =>
        {
            var todo = await todoService.GetByIdAsync(id, cancellationToken);
            return Results.Ok(todo);
        }).RequireAuthorization();

        app.MapPut("/api/todos/{id:int}", async (int id, UpdateTodoRequest request, ClaimsPrincipal user, ITodoService todoService, CancellationToken cancellationToken) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, context, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? string.Empty).ToArray());
                return Results.ValidationProblem(errors);
            }

            var response = await todoService.UpdateAsync(id, request, userId, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();

        app.MapDelete("/api/todos/{id:int}", async (int id, ClaimsPrincipal user, ITodoService todoService, CancellationToken cancellationToken) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            await todoService.DeleteAsync(id, userId, cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapPatch("/api/todos/{id:int}/status", async (int id, UpdateTodoStatusRequest request, ITodoService todoService, CancellationToken cancellationToken) =>
        {
            if (!Enum.IsDefined(typeof(TodoStatus), request.Status))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "Status", ["無効なステータス値です"] }
                });
            }

            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(request);
            if (!Validator.TryValidateObject(request, context, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .GroupBy(v => v.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(v => v.ErrorMessage ?? string.Empty).ToArray());
                return Results.ValidationProblem(errors);
            }

            var response = await todoService.UpdateStatusAsync(id, request, cancellationToken);
            return Results.Ok(response);
        }).RequireAuthorization();
    }
}
