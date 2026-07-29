using MemoRecipe.Application.Services.Alerting;

namespace MemoRecipe.Infrastructure.Tests.BackgroudServices;

// Test double capturing NotifyMassPurgeAsync calls for later assertion.
public class FakeAlertingService : IAlertingService
{
    public List<int> NotifyMassPurgeCalls { get; } = new();

    public Task NotifyMassPurgeAsync(int deletedCount, CancellationToken cancellationToken = default)
    {
        NotifyMassPurgeCalls.Add(deletedCount);
        return Task.CompletedTask;
    }

    public Task NotifyLoginFailAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyServerErrorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyBackupStaleAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
