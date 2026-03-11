using TodoApp.Shared.Models;

namespace TodoApp.Api.Data.Entities;

public class TodoItem
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public TodoStatus Status { get; set; } = TodoStatus.NotStarted;
    public Priority Priority { get; set; } = Priority.Medium;
    public int ProgressRate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public int? CategoryId { get; set; }

    public User CreatedBy { get; set; } = null!;
    public User? AssignedTo { get; set; }
}
