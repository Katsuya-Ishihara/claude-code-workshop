using System.ComponentModel.DataAnnotations;

namespace TodoApp.Shared.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "メールアドレスは必須です")]
    [EmailAddress(ErrorMessage = "メールアドレスの形式が正しくありません")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "パスワードは必須です")]
    [MinLength(8, ErrorMessage = "パスワードは8文字以上で入力してください")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "表示名は必須です")]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;
}
