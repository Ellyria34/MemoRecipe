using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Application.Tests.Fakes;
using MemoRecipe.Domain.Entities.Users;
using Microsoft.Extensions.Caching.Memory;

namespace MemoRecipe.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly FakeUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepository = new FakeUserRepository();
        _passwordHasher = new PasswordHasher();
        var jwtService = new FakeJwtService();
        var cache = new MemoryCache(new MemoryCacheOptions());

        _service = new AuthService(_userRepository, jwtService, _passwordHasher, cache);
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_WithCorrectPassword_SetsDeleteRequestedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "test",
            PasswordSalt = "",
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "GoodPass123!");
        await _userRepository.AddAsync(user);

        // Act
        var result = await _service.RequestAccountDeletionAsync(userId, "GoodPass123!");

        // Assert
        Assert.True(result);
        Assert.NotNull(user.DeleteRequestedAt);
    }

    [Fact]
    public async Task RequestAccountDeletionAsync_WithWrongPassword_KeepsDeleteRequestedAtNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "test",
            PasswordSalt = "",
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "GoodPass123!");
        await _userRepository.AddAsync(user);

        // Act
        var result = await _service.RequestAccountDeletionAsync(userId, "NoGoodPass123!");

        // Assert
        Assert.False(result);
        Assert.Null(user.DeleteRequestedAt);
    }

    [Fact]
    public async Task LoginAsync_WithDeleteRequestedAtOlderThan30Days_PurgesAccount()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "purgeme@example.com",
            Username = "purgeme",
            PasswordSalt = "",
            DeleteRequestedAt = DateTime.UtcNow.AddDays(-31)
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "MyPassword");
        await _userRepository.AddAsync(user);


        // Act
        var result = await _service.LoginAsync(user.Email, "MyPassword");

        // Assert
        Assert.Null(result.Token);
        Assert.DoesNotContain(_userRepository.All, u => u.Id == user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithDeleteRequestedAtWithinGracePeriod_CancelsDeletion()
    {
        // Arrange
        const string password = "MyPassword";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "comeback@example.com",
            Username = "comeback",
            PasswordSalt = "",
            DeleteRequestedAt = DateTime.UtcNow.AddDays(-15)
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        await _userRepository.AddAsync(user);

        // Act
        var result = await _service.LoginAsync(user.Email, password);

        // Assert
        Assert.NotNull(result.Token);
        Assert.True(result.WasDeletionCancelled);
        Assert.Null(user.DeleteRequestedAt);
    }

}
