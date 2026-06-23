namespace MemoRecipe.Application.Services.Auth;

public class LoginResult
{
    public string? Token { get; set; }
    public bool IsLockedOut { get; set; }
    public bool WasDeletionCancelled {get; set;}
}
