using MemoRecipe.Application.Configuration;
using MemoRecipe.Application.Services.Alerting;
using MemoRecipe.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Infrastructure.BackgroundServices;

// Daily background job that permanently purges user accounts marked for deletion
public class AccountPurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AccountPurgeService> _logger;
    private readonly IOptionsMonitor<AccountPurgeOptions> _optionsMonitor;

    public AccountPurgeService(
        IServiceScopeFactory scopeFactory,
        ILogger<AccountPurgeService> logger,
        IOptionsMonitor<AccountPurgeOptions> optionsMonitor)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;

        if (!options.Enabled)
        {
            _logger.LogInformation("AccountPurgeService is disabled via configuration. Exiting.");
            return;
        }

        _logger.LogInformation(
            "AccountPurgeService started. Interval={IntervalHours}h. Grace={PurgeAfterDays}d.",
            options.IntervalHours, options.PurgeAfterDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecutePurgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account purge run failed. Will retry at next interval.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(options.IntervalHours), stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Normal shutdown when the app stops
                break;
            }
        }
    }

    // Marked internal to allow direct unit-testing without waiting the ExecuteAsync loop.
    internal async Task ExecutePurgeAsync(CancellationToken cancellationToken)
    {
        var options = _optionsMonitor.CurrentValue;
        var cutoff = DateTime.UtcNow.AddDays(-options.PurgeAfterDays);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        var alerting = scope.ServiceProvider.GetRequiredService<IAlertingService>();

        // Wrap the destructive DELETE in a transaction so a partial cascade failure
        // rolls back everything (safer than a half-purged DB state).
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Re-select users inside the transaction (protects against a race with
            // a user who cancels their deletion in the milliseconds before this runs).
            var expiredUsers = await db.Users
                .Where(u => u.DeleteRequestedAt != null && u.DeleteRequestedAt < cutoff)
                .ToListAsync(cancellationToken);

            if (expiredUsers.Count == 0)
            {
                _logger.LogInformation("Account purge run: no expired accounts to purge.");
                await transaction.CommitAsync(cancellationToken);
                await alerting.NotifyMassPurgeAsync(0, cancellationToken);
                return;
            }

            var purgedIds = expiredUsers.Select(u => u.Id).ToList();

            db.Users.RemoveRange(expiredUsers);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Account purge run completed. Purged {Count} accounts: {UserIds}",
                expiredUsers.Count, purgedIds);

            await alerting.NotifyMassPurgeAsync(expiredUsers.Count, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
