using MemoRecipe.Application.Attributes;

namespace MemoRecipe.Application.DTOs.Auth;

public class DeleteAccountDto
{
    [SensitiveData]
    public string Password { get; set; } = string.Empty;
}
