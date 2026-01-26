namespace MemoRecipe.Application.DTOs.Auth;

public class AuthUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
}
