using TodoApp.Shared.Models;

namespace TodoApp.Api.Data.Entities;

public class User
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TodoItem> CreatedTodos { get; set; } = [];
    public ICollection<TodoItem> AssignedTodos { get; set; } = [];
}
