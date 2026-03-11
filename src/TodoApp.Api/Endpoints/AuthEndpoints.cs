using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
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
