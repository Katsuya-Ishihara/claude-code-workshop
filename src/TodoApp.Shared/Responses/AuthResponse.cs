namespace TodoApp.Shared.Responses;

public class AuthResponse
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Token { get; set; }
}
