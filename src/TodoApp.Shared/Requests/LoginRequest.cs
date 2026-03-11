using System.ComponentModel.DataAnnotations;

namespace TodoApp.Shared.Requests;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    [Required]
    [StringLength(128)]
    public required string Password { get; set; }
}
