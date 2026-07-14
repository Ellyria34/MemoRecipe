using MemoRecipe.Application.Notifications;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Application.Services.Alerting;

public class AlertingService : IAlertingService
{
    private readonly INotificationChannel _notificationChannel;
    private readonly AlertingOptions _options;

    public AlertingService(INotificationChannel notificationChannel, IOptions<AlertingOptions> options)
    {
        ArgumentNullException.ThrowIfNull(notificationChannel);
        ArgumentNullException.ThrowIfNull(options);

        _notificationChannel = notificationChannel;
        _options = options.Value;
    }

    public async Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default)
    {
        if (deletedCount < _options.MassPurgeCritical)
        {
            return ;
        }
        Alert alert = new Alert(
            AlertLevel.Critical, 
            "Mass purge alert",
            $"{deletedCount} accounts were purged (threshold: {_options.MassPurgeCritical})",
            DateTimeOffset.UtcNow);
            
        await _notificationChannel.SendAsync(alert, cancellationToken);

    }
}