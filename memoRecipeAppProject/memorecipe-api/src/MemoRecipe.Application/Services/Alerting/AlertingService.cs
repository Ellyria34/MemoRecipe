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
}