using TodoApp.Api.Services;
using TodoApp.Shared.Requests;

namespace TodoApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
        {
            var result = await authService.LoginAsync(request, cancellationToken);
            return result is null
                ? Results.Unauthorized()
                : Results.Ok(result);
        });
    }
}
