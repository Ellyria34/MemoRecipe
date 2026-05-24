using System.Security.Claims;
using MemoRecipe.Application.DTOs.Auth;
using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Users;
using Microsoft.Extensions.Caching.Memory;

namespace MemoRecipe.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly PasswordHasher _passwordHasher;

    private readonly IMemoryCache _cache;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, PasswordHasher passwordHasher, IMemoryCache cache)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _cache = cache;
    }

    public async Task<string?> RegisterAsync(RegisterDto dto)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            Username = dto.Username,
            PasswordHash = "",
            PasswordSalt = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        // Vérifier email déjà utilisé ?
        if (await _userRepository.EmailExistsAsync(dto.Email))
            return null;

        // Hash du mot de passe
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return _jwtService.GenerateToken(user);
    }

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        if (_cache.TryGetValue($"login-fail:{email}", out int failCount) && failCount >= 5)
        {
            return new LoginResult { IsLockedOut = true };
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            var newCount = failCount + 1;
            _cache.Set($"login-fail:{email}", newCount, TimeSpan.FromMinutes(15));

            return new LoginResult { Token = null };
        }

        if (!_passwordHasher.Verify(user, user.PasswordHash, password, user.PasswordSalt))
        {
            var newCount = failCount + 1;
            _cache.Set($"login-fail:{email}", newCount, TimeSpan.FromMinutes(15));

            return new LoginResult { Token = null };
        }

        if (_passwordHasher.NeedsRehash(user.PasswordSalt))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                user.PasswordSalt = "";
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();
            }

        _cache.Remove($"login-fail:{email}");
        return new LoginResult { Token = _jwtService.GenerateToken(user) };
    }

   public async Task<AuthUserDto?> GetCurrentUserAsync(ClaimsPrincipal user)
    {
        var userId = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
            return null;

        var userGuid = Guid.Parse(userId);

        var entity = await _userRepository.GetByIdAsync(userGuid);
        if (entity == null)
            return null;

        return new AuthUserDto
        {
            Id = entity.Id,
            Email = entity.Email,
            Username = entity.Username,
        };
    }
}
