using TodoApp.Shared.Models;

namespace TodoApp.Shared.Responses;

public class TodoResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TodoStatus Status { get; set; }
    public Priority Priority { get; set; }
    public int ProgressRate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CreatedByUserId { get; set; }
    public int? AssignedToUserId { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string? AssignedToName { get; set; }
}
