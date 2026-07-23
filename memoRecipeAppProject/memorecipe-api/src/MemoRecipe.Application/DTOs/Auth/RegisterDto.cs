using MemoRecipe.Application.Attributes;

namespace MemoRecipe.Application.DTOs.Auth;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    [SensitiveData]
    public string Password { get; set; } = string.Empty;
}
