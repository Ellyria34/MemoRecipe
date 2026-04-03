using System.Security.Claims;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Services.Auth;

public interface IAuthService
{
    Task<string?> RegisterAsync(RegisterDto dto);
    Task<LoginResult> LoginAsync(string email, string password);
    Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal user);
}
