using MemoRecipe.Application.Notifications;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Application.Services.Alerting;

public class AlertingService : IAlertingService
{
    private readonly INotificationChannel _notificationChannel;
    private readonly AlertingOptions _options;
    private readonly IMemoryCache _cache;

    public AlertingService(INotificationChannel notificationChannel, IOptions<AlertingOptions> options, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(notificationChannel);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(cache);

        _notificationChannel = notificationChannel;
        _options = options.Value;
        _cache = cache;
    }

    public async Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default)
    {
        if (deletedCount < _options.MassPurgeCritical)
        {
            return;
        }
        Alert alert = new Alert(
            AlertLevel.Critical,
            "Mass purge alert",
            $"{deletedCount} accounts were purged (threshold: {_options.MassPurgeCritical})",
            DateTimeOffset.UtcNow);

        await _notificationChannel.SendAsync(alert, cancellationToken);

    }

    public async Task NotifyLoginFailAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "alerting:login-fail-count";
        var count = _cache.Get<int>(cacheKey);
        count++;
        _cache.Set(cacheKey, count, _options.LoginFailStormWindow);

        if (count != _options.LoginFailStormCritical)
        {
            return;
        }

        Alert alert = new Alert(
            AlertLevel.Critical,
            "Login fail storm detected",
            $"{count} login failures in the last {_options.LoginFailStormWindow.TotalMinutes:F0} min",
            DateTimeOffset.UtcNow);

        await _notificationChannel.SendAsync(alert, cancellationToken);
    }

    public async Task NotifyServerErrorAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "alerting:server-error-spike-count";
        var count = _cache.Get<int>(cacheKey);
        count++;
        _cache.Set(cacheKey, count, _options.ServerErrorSpikeWindow);

        if (count != _options.ServerErrorSpikeCritical)
        {
            return;
        }

        Alert alert = new Alert(
            AlertLevel.Critical,
            "Server error spike detected",
            $"{count} server error failures in the last {_options.ServerErrorSpikeWindow.TotalMinutes:F0} min",
            DateTimeOffset.UtcNow);

        await _notificationChannel.SendAsync(alert, cancellationToken);
    }

    public async Task NotifyBackupStaleAsync(CancellationToken cancellationToken = default)
    {
        DateTime mostRecentUtc = DateTime.MinValue;

        if (Directory.Exists(_options.BackupPath))
        {
            foreach (var file in Directory.EnumerateFiles(_options.BackupPath))
            {
                var writeTime = File.GetLastWriteTimeUtc(file);
                if (writeTime > mostRecentUtc)
                {
                    mostRecentUtc = writeTime;
                }
            }
        }

        var age = DateTime.UtcNow - mostRecentUtc;
        if (age <= _options.BackupStaleAfter)
        {
            return;
        }

        var message = mostRecentUtc == DateTime.MinValue
        ? $"No backup file found in {_options.BackupPath}"
        : $"Latest backup is {age.TotalHours:F0}h old (threshold: {_options.BackupStaleAfter.TotalHours:F0}h)";

        var alert = new Alert(
            AlertLevel.Critical,
            "Backup stale",
            message,
            DateTimeOffset.UtcNow);

        await _notificationChannel.SendAsync(alert, cancellationToken);
    }
}