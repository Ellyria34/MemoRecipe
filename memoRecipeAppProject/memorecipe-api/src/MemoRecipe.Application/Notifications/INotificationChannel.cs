namespace MemoRecipe.Application.Notifications;

public interface INotificationChannel
{
    Task SendAsync(Alert alert, CancellationToken cancellationToken = default);
}
