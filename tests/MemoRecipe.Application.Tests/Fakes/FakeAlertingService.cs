using MemoRecipe.Application.Services.Alerting;

namespace MemoRecipe.Application.Tests.Fakes;

public class FakeAlertingService : IAlertingService
{
    public int LoginFailCallCount { get; private set; }
    public int MassPurgeCallCount { get; private set; }
    public int ServerErrorCallCount { get; private set; }
    public int BackupStaleCallCount { get; private set; }

    public Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default)
    {
        MassPurgeCallCount++;
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
}
