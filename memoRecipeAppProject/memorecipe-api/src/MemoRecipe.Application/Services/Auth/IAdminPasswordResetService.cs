namespace MemoRecipe.Application.Services.Auth;

public interface IAdminPasswordResetService
{
    Task<PasswordResetResult> ResetAsync(string email, string newPassword, CancellationToken cancellationToken = default);
}