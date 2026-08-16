using MemoRecipe.Application.Configuration;
using MemoRecipe.Application.Services.Alerting;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Infrastructure.BackgroundServices;
using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Tests.Shared.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace MemoRecipe.Infrastructure.Tests.BackgroundServices;

public class AccountPurgeServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16-alpine").Build();


    private ServiceProvider _serviceProvider = null!;
    private FakeAlertingService _alerting = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddDbContext<MemoRecipeDbContext>(options =>
            options.UseNpgsql(_postgres.GetConnectionString()));

        _alerting = new FakeAlertingService();
        services.AddScoped<IAlertingService>(_ => _alerting);

        services.Configure<AccountPurgeOptions>(opt =>
        {
            opt.Enabled = true;
            opt.PurgeAfterDays = 30;
            opt.IntervalHours = 24;
        });

        _serviceProvider = services.BuildServiceProvider();

        // Apply EF Core migrations to the real container DB
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceProvider.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private AccountPurgeService CreateService()
    {
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = _serviceProvider.GetRequiredService<ILogger<AccountPurgeService>>();
        var options = _serviceProvider.GetRequiredService<IOptionsMonitor<AccountPurgeOptions>>();
        return new AccountPurgeService(scopeFactory, logger, options);
    }

    private static User BuildUser(string email, DateTime? deleteRequestedAt)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = email.Split('@')[0],
            PasswordHash = "",
            PasswordSalt = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeleteRequestedAt = deleteRequestedAt
        };
    }

    [Fact]
    public async Task ExecutePurgeAsync_WithExpiredAccounts_PurgesThemAndNotifiesAlerting()
    {
        // Arrange - seed 2 users past the 30-day grace period
        using (var seedScope = _serviceProvider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            db.Users.Add(BuildUser("expired1@test.com", DateTime.UtcNow.AddDays(-31)));
            db.Users.Add(BuildUser("expired2@test.com", DateTime.UtcNow.AddDays(-45)));
            await db.SaveChangesAsync();
        }

        // Act
        var service = CreateService();
        await service.ExecutePurgeAsync(CancellationToken.None);

        // Assert DB - both users are gone
        using var assertScope = _serviceProvider.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        Assert.Empty(await assertDb.Users.AsNoTracking().ToListAsync());

        // Assert alerting - one call with count = 2
        Assert.Single(_alerting.NotifyMassPurgeCalls);
        Assert.Equal(2, _alerting.NotifyMassPurgeCalls[0]);
    }

    [Fact]
    public async Task ExecutePurgeAsync_WithAccountsUnderGracePeriod_DoesNotPurge()
    {
        // Arrange - 2 users still within the 30-day grace period
        using (var seedScope = _serviceProvider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            db.Users.Add(BuildUser("recent1@test.com", DateTime.UtcNow.AddDays(-15)));
            db.Users.Add(BuildUser("recent2@test.com", DateTime.UtcNow.AddDays(-29)));
            await db.SaveChangesAsync();
        }

        // Act
        var service = CreateService();
        await service.ExecutePurgeAsync(CancellationToken.None);

        // Assert DB - both users still present
        using var assertScope = _serviceProvider.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        Assert.Equal(2, await assertDb.Users.AsNoTracking().CountAsync());

        // Assert alerting - one call with count = 0 (heartbeat)
        Assert.Single(_alerting.NotifyMassPurgeCalls);
        Assert.Equal(0, _alerting.NotifyMassPurgeCalls[0]);
    }

    [Fact]
    public async Task ExecutePurgeAsync_WithNoDeletionRequests_NotifiesZeroCount()
    {
        // Arrange - user without any deletion request
        using (var seedScope = _serviceProvider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            db.Users.Add(BuildUser("active@test.com", deleteRequestedAt: null));
            await db.SaveChangesAsync();
        }

        // Act
        var service = CreateService();
        await service.ExecutePurgeAsync(CancellationToken.None);

        // Assert
        using var assertScope = _serviceProvider.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        Assert.Single(await assertDb.Users.AsNoTracking().ToListAsync());

        Assert.Single(_alerting.NotifyMassPurgeCalls);
        Assert.Equal(0, _alerting.NotifyMassPurgeCalls[0]);
    }

    [Fact]
    public async Task ExecutePurgeAsync_WithMixedAccountsAndRecipes_PurgesOnlyExpiredWithCascade()
    {
        // Arrange - user1 expired with 2 recipes, user2 active with 1 recipe.
        // The cascade delete on Users -> Recipes -> Ingredients must fire for user1.
        var expiredUser = BuildUser("expired@test.com", DateTime.UtcNow.AddDays(-31));
        var activeUser = BuildUser("active@test.com", deleteRequestedAt: null);

        using (var seedScope = _serviceProvider.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            db.Users.Add(expiredUser);
            db.Users.Add(activeUser);

            var expiredRecipe1 = new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Expired user's recipe 1",
                UserId = expiredUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Ingredients = new List<Ingredient>
                {
                    new() { Id = Guid.NewGuid(), Name = "Flour", Quantity = 500, Unit = "g" }
                }
            };
            var expiredRecipe2 = new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Expired user's recipe 2",
                UserId = expiredUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var activeRecipe = new Recipe
            {
                Id = Guid.NewGuid(),
                Title = "Active user's recipe",
                UserId = activeUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Recipes.AddRange(expiredRecipe1, expiredRecipe2, activeRecipe);
            await db.SaveChangesAsync();
        }

        // Act
        var service = CreateService();
        await service.ExecutePurgeAsync(CancellationToken.None);

        // Assert - only active user + his recipe remain, expired user's data is gone
        using var assertScope = _serviceProvider.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();

        var users = await assertDb.Users.AsNoTracking().ToListAsync();
        Assert.Single(users);
        Assert.Equal(activeUser.Id, users[0].Id);

        var recipes = await assertDb.Recipes.AsNoTracking().ToListAsync();
        Assert.Single(recipes);
        Assert.Equal(activeUser.Id, recipes[0].UserId);

        // No orphan ingredients from the purged recipe
        var ingredients = await assertDb.Set<Ingredient>().AsNoTracking().ToListAsync();
        Assert.Empty(ingredients);

        Assert.Single(_alerting.NotifyMassPurgeCalls);
        Assert.Equal(1, _alerting.NotifyMassPurgeCalls[0]);
    }
}
