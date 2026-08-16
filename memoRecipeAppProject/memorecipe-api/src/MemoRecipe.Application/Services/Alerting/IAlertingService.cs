
namespace MemoRecipe.Application.Services.Alerting;

public interface IAlertingService
{
    Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default);
    Task NotifyLoginFailAsync(CancellationToken cancellationToken = default);
    Task NotifyServerErrorAsync(CancellationToken cancellationToken = default);
    Task NotifyBackupStaleAsync(CancellationToken cancellationToken = default);
    Task NotifyAiCostDailyAsync(string provider, long tokensUsed, long threshold, CancellationToken cancellationToken = default);
    Task NotifyAiCostWeeklyAsync(string provider, long tokensUsed, long threshold, CancellationToken cancellationToken = default);
}
