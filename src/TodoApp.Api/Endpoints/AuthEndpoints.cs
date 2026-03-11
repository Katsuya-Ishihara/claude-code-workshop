using System.ComponentModel.DataAnnotations;
using TodoApp.Api.Services;
using TodoApp.Shared.Requests;

namespace TodoApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
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

            var response = await authService.RegisterAsync(request);
            return Results.Created($"/api/users/{response.UserId}", response);
        });
    }
}
