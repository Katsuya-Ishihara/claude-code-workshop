using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public interface IUserService
{
    Task<List<UserSummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default);
}
