using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TodoApp.Api.Services;
using TodoApp.Shared.Requests;

namespace TodoApp.Api.Endpoints;

public static class TodoEndpoints
{
    public static void MapTodoEndpoints(this WebApplication app)
    {
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
    }
}
