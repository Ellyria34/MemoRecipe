
namespace MemoRecipe.Application.Services.Alerting;

public interface IAlertingService
{
    Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default);
    Task NotifyLoginFailAsync(CancellationToken cancellationToken = default);
}
