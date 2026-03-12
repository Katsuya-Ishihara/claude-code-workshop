using System.ComponentModel.DataAnnotations;
using TodoApp.Shared.Models;

namespace TodoApp.Shared.Requests;

public class UpdateTodoRequest
{
    [Required(ErrorMessage = "タイトルは必須です")]
    [MaxLength(200, ErrorMessage = "タイトルは200文字以内で入力してください")]
    public required string Title { get; set; }

    public string? Description { get; set; }
    public Priority? Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public int? AssignedToUserId { get; set; }
}
