using TodoApp.Shared.Models;

namespace TodoApp.Shared.Requests;

public class GetTodosRequest
{
    public TodoStatus? Status { get; set; }
    public Priority? Priority { get; set; }
    public int? AssignedToUserId { get; set; }
    public string? Keyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
