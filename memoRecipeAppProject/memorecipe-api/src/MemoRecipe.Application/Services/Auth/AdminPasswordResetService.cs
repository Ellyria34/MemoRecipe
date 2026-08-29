using MemoRecipe.Application.Helpers;
using MemoRecipe.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace MemoRecipe.Application.Services.Auth;

public class AdminPasswordResetService : IAdminPasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<AdminPasswordResetService> _logger;

    public AdminPasswordResetService(IUserRepository userRepository, PasswordHasher passwordHasher, ILogger<AdminPasswordResetService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PasswordResetResult> ResetAsync(string email, string newPassword, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);

        // Find user
        var user = await _userRepository.GetByEmailAsync(normalizedEmail);

        // Not found → log + return
        if (user is null)
        {
            _logger.LogWarning("AdminPasswordResetAttempted - user not found for email {Email}", EmailMasker.Mask(normalizedEmail));
            return PasswordResetResult.UserNotFound;
        }

        // Hash new password + clear legacy salt
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.PasswordSalt = "";

        // Persist
        _userRepository.Update(user);
        await _userRepository.SaveChangesAsync();

        // Audit log (WARNING level for admin action visibility, never log password)
        _logger.LogWarning("AdminPasswordResetPerformed - user {UserId} email {Email}", user.Id, EmailMasker.Mask(normalizedEmail));

        return PasswordResetResult.Success;
    }
}