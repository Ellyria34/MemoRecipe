using MemoRecipe.Application.Services.Alerting;

namespace MemoRecipe.Tests.Shared.Fakes;

public class FakeAlertingService : IAlertingService
{
    public int LoginFailCallCount { get; private set; }
    public List<int> NotifyMassPurgeCalls { get; } = new();
    public int ServerErrorCallCount { get; private set; }
    public int BackupStaleCallCount { get; private set; }
    public int AiCostDailyCallCount { get; private set; }
    public int AiCostWeeklyCallCount { get; private set; }

    public Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default)
    {
        NotifyMassPurgeCalls.Add(deletedCount);
        return Task.CompletedTask;
    }

    public Task NotifyLoginFailAsync(CancellationToken cancellationToken = default)
    {
        LoginFailCallCount++;
        return Task.CompletedTask;
    }

    public Task NotifyServerErrorAsync(CancellationToken cancellationToken = default)
    {
        ServerErrorCallCount++;
        return Task.CompletedTask;
    }

    public Task NotifyBackupStaleAsync(CancellationToken cancellationToken = default)
    {
        BackupStaleCallCount++;
        return Task.CompletedTask;
    }

    public Task NotifyAiCostDailyAsync(string provider, long tokensUsed, long threshold, CancellationToken cancellationToken = default)
    {
        AiCostDailyCallCount++;
        return Task.CompletedTask;
    }

    public Task NotifyAiCostWeeklyAsync(string provider, long tokensUsed, long threshold, CancellationToken cancellationToken = default)
    {
        AiCostWeeklyCallCount++;
        return Task.CompletedTask;
    }
}