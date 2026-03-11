using TodoApp.Shared.Models;

namespace TodoApp.Shared.Responses;

public record UserResponse(int Id, string Email, string DisplayName, UserRole Role);
