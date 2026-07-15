using MemoRecipe.Application.DTOs.Auth;
using MemoRecipe.Application.Services.Auth;
using MemoRecipe.Application.Tests.Fakes;
using MemoRecipe.Domain.Entities.Users;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoRecipe.Application.Tests.Services;

public class AuthServiceTests
{
    private const string TestIpAddress = "127.0.0.1";
    private readonly FakeUserRepository _userRepository;
    private readonly PasswordHasher _passwordHasher;
    private readonly AuthService _service;
    private readonly FakeAlertingService _alertingService;

    public AuthServiceTests()
    {
        _userRepository = new FakeUserRepository();
        _passwordHasher = new PasswordHasher();
        var jwtService = new FakeJwtService();
        var cache = new MemoryCache(new MemoryCacheOptions());
        _alertingService = new FakeAlertingService();
        _service = new AuthService(
            _userRepository,
            jwtService,
            _passwordHasher,
            cache,
            NullLogger<AuthService>.Instance,
            _alertingService);
    }

    #region LoginTest
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
        var result = await _service.RequestAccountDeletionAsync(userId, "GoodPass123!", TestIpAddress);

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
        var result = await _service.RequestAccountDeletionAsync(userId, "NoGoodPass123!", TestIpAddress);

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
        var result = await _service.LoginAsync(user.Email, "MyPassword", TestIpAddress);

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
        var result = await _service.LoginAsync(user.Email, password, TestIpAddress);

        // Assert
        Assert.NotNull(result.Token);
        Assert.True(result.WasDeletionCancelled);
        Assert.Null(user.DeleteRequestedAt);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_TriggersLoginFailAlert()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "test",
            PasswordSalt = ""
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, "GoodPass123!");
        await _userRepository.AddAsync(user);

        // Act
        await _service.LoginAsync("test@example.com", "WrongPass456!", TestIpAddress);

        // Assert
        Assert.Equal(1, _alertingService.LoginFailCallCount);
    }

    #endregion

    #region ResgisterTest
    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsTokenAndPersistsUser()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "newuser@example.com",
            Username = "newuser",
            Password = "StrongPass123!"
        };

        // Act
        var token = await _service.RegisterAsync(dto, TestIpAddress);

        // Assert
        Assert.NotNull(token);
        Assert.Contains(_userRepository.All, u => u.Email == dto.Email);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsNullAndDoesNotAddUser()
    {
        // Arrange : ajouter un user avec l'email
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "taken@example.com",
            Username = "existing",
            PasswordHash = "",
            PasswordSalt = ""
        };
        await _userRepository.AddAsync(existingUser);

        var dto = new RegisterDto
        {
            Email = "taken@example.com",  // même email
            Username = "newattempt",
            Password = "SomePass123!"
        };

        // Act
        var token = await _service.RegisterAsync(dto, TestIpAddress);

        // Assert
        Assert.Null(token);
        Assert.Single(_userRepository.All);  // seul le user existant est là
    }

    [Fact]
    public async Task RegisterAsync_WithValidDto_StoresHashedPassword()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "hashtest@example.com",
            Username = "hashtest",
            Password = "MySecret123!"
        };

        // Act
        await _service.RegisterAsync(dto, TestIpAddress);

        // Assert
        var createdUser = _userRepository.All.Single(u => u.Email == dto.Email);
        Assert.NotEqual(dto.Password, createdUser.PasswordHash);
        Assert.True(_passwordHasher.Verify(createdUser, createdUser.PasswordHash, dto.Password, createdUser.PasswordSalt));
    }
    #endregion
}
