using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Api.Services;
using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService, CancellationToken cancellationToken) =>
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

            var response = await authService.RegisterAsync(request, cancellationToken);
            return Results.Created($"/api/users/{response.UserId}", response);
        });

        app.MapGet("/api/auth/me", async (ClaimsPrincipal user, TodoAppDbContext db) =>
        {
            var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userIdClaim is null || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var dbUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (dbUser is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new UserResponse(dbUser.Id, dbUser.Email, dbUser.DisplayName, dbUser.Role));
        }).RequireAuthorization();
    }
}
