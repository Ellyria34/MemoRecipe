using MemoRecipe.Application.Helpers;
using MemoRecipe.Application.Repositories;
using Microsoft.Extensions.Logging;

namespace MemoRecipe.Application.Services.Auth;

public class AdminPasswordResetService : IAdminPasswordResetService
{
    private readonly IUserRepository _userRepository;
    private readonly PasswordHasher  _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    public AdminPasswordResetService (IUserRepository userRepository, PasswordHasher  passwordHasher, ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<PasswordResetResult> ResetAsync(string email, string newPassword, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = EmailNormalizer.Normalize(email);
        // 2. GetByEmailAsync
        // 3. If null return UserNotFound
        // 4. HashPassword + reset PasswordSalt
        // 5. Update + SaveChangesAsync
        // 6. Log the admin action (masked email, no password)
        // 7. return Success
        throw new NotImplementedException();

    }
}