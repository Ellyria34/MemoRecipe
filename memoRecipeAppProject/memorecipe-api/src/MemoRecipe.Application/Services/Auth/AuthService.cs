using System.Security.Claims;
using MemoRecipe.Application.DTOs.Auth;
using MemoRecipe.Application.Helpers;
using MemoRecipe.Application.Repositories;
using MemoRecipe.Domain.Entities.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace MemoRecipe.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly PasswordHasher _passwordHasher;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository userRepository, IJwtService jwtService, PasswordHasher passwordHasher, IMemoryCache cache, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> RegisterAsync(RegisterDto dto, string ipAddress)
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
        // Check if the email address is already in use?
        if (await _userRepository.EmailExistsAsync(dto.Email))
        {
            _logger.LogWarning("{EventType} — masked email {MaskedEmail} from {IpAddress}",
                "RegisterFailedEmailTaken", EmailMasker.Mask(dto.Email), ipAddress);
            return null;
        }

        // Password hash
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("{EventType} — user {UserId} masked email {MaskedEmail} from {IpAddress}",
            "RegisterSuccess", user.Id, EmailMasker.Mask(dto.Email), ipAddress);

        return _jwtService.GenerateToken(user);
    }

    public async Task<LoginResult> LoginAsync(string email, string password, string ipAddress)
    {
        if (_cache.TryGetValue($"login-fail:{email}", out int failCount) && failCount >= 5)
        {
            _logger.LogWarning("{EventType} — masked email {MaskedEmail} from {IpAddress}",
                "AccountLocked", EmailMasker.Mask(email), ipAddress);
            return new LoginResult { IsLockedOut = true };
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            var newCount = failCount + 1;
            _cache.Set($"login-fail:{email}", newCount, TimeSpan.FromMinutes(15));

            _logger.LogWarning("{EventType} — masked email {MaskedEmail} from {IpAddress}",
                "LoginFailedUserNotFound", EmailMasker.Mask(email), ipAddress);
            return new LoginResult { Token = null };
        }

        if (!_passwordHasher.Verify(user, user.PasswordHash, password, user.PasswordSalt))
        {
            var newCount = failCount + 1;
            _cache.Set($"login-fail:{email}", newCount, TimeSpan.FromMinutes(15));

            _logger.LogWarning("{EventType} — user {UserId} masked email {MaskedEmail} from {IpAddress}",
                "LoginFailedWrongPassword", user.Id, EmailMasker.Mask(email), ipAddress);
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

        // Check account deletion state
        var wasDeletionCancelled = false;
        if (user.DeleteRequestedAt != null)
        {
            if (user.DeleteRequestedAt.Value < DateTime.UtcNow.AddDays(-30))
            {
                // Grace period expired → purge account definitively
                _userRepository.Delete(user);
                await _userRepository.SaveChangesAsync();
                _logger.LogWarning("{EventType} — user {UserId} from {IpAddress}",
                    "AccountAutoPurged", user.Id, ipAddress);
                return new LoginResult { Token = null };
            }

            // Within grace period → cancel deletion
            user.DeleteRequestedAt = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
            wasDeletionCancelled = true;
            _logger.LogInformation("{EventType} — user {UserId} from {IpAddress}", 
                "AccountDeletionCancelled", user.Id, ipAddress);
        }

        _logger.LogInformation("{EventType} — user {UserId} from {IpAddress}",
            "LoginSuccess", user.Id, ipAddress);

        return new LoginResult
        {
            Token = _jwtService.GenerateToken(user),
            WasDeletionCancelled = wasDeletionCancelled
        };
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

    public async Task<bool> RequestAccountDeletionAsync(Guid userId, string password, string ipAddress)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("{EventType} — user {UserId} from {IpAddress}",
                "AccountDeletionUserNotFound", userId, ipAddress);
            return false;
        }

        if (!_passwordHasher.Verify(user, user.PasswordHash, password, user.PasswordSalt))
        {
            _logger.LogWarning("{EventType} — user {UserId} from {IpAddress}",
                "AccountDeletionFailedWrongPassword", userId, ipAddress);
            return false;
        }

        user.DeleteRequestedAt = DateTime.UtcNow;
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        _logger.LogWarning("{EventType} — user {UserId} from {IpAddress}",
            "AccountDeletionRequested", userId, ipAddress);
        return true;

    }

}
