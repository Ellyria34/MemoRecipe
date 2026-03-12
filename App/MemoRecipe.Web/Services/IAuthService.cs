namespace MemoRecipe.Web.Services;

public interface IAuthService
{
    Task<bool> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string email , string userName, string password);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
}
