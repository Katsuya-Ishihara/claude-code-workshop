using TodoApp.Api.Services;

namespace TodoApp.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        app.MapGet("/api/users", async (IUserService userService, CancellationToken cancellationToken) =>
        {
            var users = await userService.GetAllAsync(cancellationToken);
            return Results.Ok(users);
        }).RequireAuthorization();
    }
}
