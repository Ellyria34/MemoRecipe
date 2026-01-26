using System.Security.Claims;
using MemoRecipe.Application.DTOs.Auth;

namespace MemoRecipe.Application.Services.Auth;

public interface IAuthService
{
    Task<AuthUserDto?> RegisterAsync(RegisterDto dto);
    Task<string?> LoginAsync(string email, string password);
    Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal user);
}
