using TodoApp.Shared.Models;

namespace TodoApp.Shared.Responses;

public record UserSummaryResponse(int Id, string Email, string DisplayName, UserRole Role, DateTime CreatedAt);
