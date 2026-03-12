using System.ComponentModel.DataAnnotations;
using TodoApp.Shared.Models;

namespace TodoApp.Shared.Requests;

public class UpdateTodoStatusRequest
{
    [Required(ErrorMessage = "ステータスは必須です")]
    public TodoStatus Status { get; set; }
}
