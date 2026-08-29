using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Application.Tests.Fakes;
using MemoRecipe.Domain.Entities.Users;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoRecipe.Application.Tests.Services;

public class AdminPasswordResetServiceTests
{
    private readonly FakeUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly AdminPasswordResetService _service;

    public AdminPasswordResetServiceTests()
    {
        _userRepository = new FakeUserRepository();
        _passwordHasher = new PasswordHasher();
        _service = new AdminPasswordResetService(
            _userRepository,
            _passwordHasher,
            NullLogger<AdminPasswordResetService>.Instance);
    }

    [Fact]
    public async Task ResetAsync_WithNonExistingEmail_ReturnsUserNotFound()
    {
        // Arrange
        // No user seeded

        // Act
        var result = await _service.ResetAsync("ghost@example.com", "NewPass123!");

        // Assert
        Assert.Equal(PasswordResetResult.UserNotFound, result);
    }

    [Fact]
    public async Task ResetAsync_WithExistingEmail_ReturnsSuccess()
    {
        // Arrange
        var user = await SeedUserAsync("known@example.com", "OldPass123!");

        // Act
        var result = await _service.ResetAsync("known@example.com", "NewPass456!");

        // Assert
        Assert.Equal(PasswordResetResult.Success, result);
    }

    [Fact]
    public async Task ResetAsync_WithExistingEmail_UpdatesPasswordHash()
    {
        // Arrange
        var user = await SeedUserAsync("known@example.com", "OldPass123!");
        var originalHash = user.PasswordHash;

        // Act
        await _service.ResetAsync("known@example.com", "NewPass456!");

        // Assert
        Assert.NotEqual(originalHash, user.PasswordHash);
        Assert.NotEmpty(user.PasswordHash);
    }

    [Fact]
    public async Task ResetAsync_WithExistingEmail_ClearsPasswordSalt()
    {
        // Arrange - seed user with legacy salt to simulate pre-migration state
        var user = await SeedUserAsync("legacy@example.com", "OldPass123!");
        user.PasswordSalt = "someLegacySalt==";

        // Act
        await _service.ResetAsync("legacy@example.com", "NewPass456!");

        // Assert
        Assert.Equal("", user.PasswordSalt);
    }

    [Fact]
    public async Task ResetAsync_WithMixedCaseEmail_FindsAndUpdatesLowercaseUser()
    {
        // Arrange - user stored lowercase in DB (invariant after P0-8)
        await SeedUserAsync("known@example.com", "OldPass123!");

        // Act - admin types email in mixed case
        var result = await _service.ResetAsync("Known@EXAMPLE.com", "NewPass456!");

        // Assert
        Assert.Equal(PasswordResetResult.Success, result);
    }

    [Fact]
    public async Task ResetAsync_WithNewPassword_HashIsVerifiableByPasswordHasher()
    {
        // Arrange - the critical end-to-end contract: reset produces a hash
        // that the standard login flow (PasswordHasher.Verify) will accept.
        var user = await SeedUserAsync("known@example.com", "OldPass123!");
        const string newPassword = "BrandNewPass789!";

        // Act
        await _service.ResetAsync("known@example.com", newPassword);

        // Assert - the new hash must verify against the new password
        var verified = _passwordHasher.Verify(user, user.PasswordHash, newPassword, user.PasswordSalt);
        Assert.True(verified);

        // And the old password must no longer verify
        var oldVerified = _passwordHasher.Verify(user, user.PasswordHash, "OldPass123!", user.PasswordSalt);
        Assert.False(oldVerified);
    }

    private async Task<User> SeedUserAsync(string email, string initialPassword)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = email.Split('@')[0],
            PasswordSalt = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, initialPassword);
        await _userRepository.AddAsync(user);
        return user;
    }
}
