using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Data;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public class UserService(TodoAppDbContext dbContext) : IUserService
{
    public async Task<List<UserSummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserSummaryResponse(u.Id, u.Email, u.DisplayName, u.Role, u.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
