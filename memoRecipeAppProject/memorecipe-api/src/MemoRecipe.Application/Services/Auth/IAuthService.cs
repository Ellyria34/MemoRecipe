using System.Security.Claims;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Services.Auth;

public interface IAuthService
{
    Task<string?> RegisterAsync(RegisterDto dto, string ipAddress);
    Task<LoginResult> LoginAsync(string email, string password, string ipAddress);
    Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal user);
    Task<bool> RequestAccountDeletionAsync(Guid userId, string password, string ipAddress);
}
