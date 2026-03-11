using TodoApp.Shared.Requests;
using TodoApp.Shared.Responses;

namespace TodoApp.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
