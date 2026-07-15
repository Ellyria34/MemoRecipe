using MemoRecipe.Application.Notifications;

namespace MemoRecipe.Application.Tests.Fakes;

public class FakeNotificationChannel : INotificationChannel
{
    public List<Alert> SentAlerts { get; } = [];

    public Task SendAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        SentAlerts.Add(alert);
        return Task.CompletedTask;
    }
}
