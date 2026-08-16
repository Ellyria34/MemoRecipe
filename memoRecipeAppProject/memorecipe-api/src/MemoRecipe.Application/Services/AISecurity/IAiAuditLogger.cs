using MemoRecipe.Application.Services.Monitoring;

namespace MemoRecipe.Application.Services.AISecurity;

public interface IAiAuditLogger
{
    Task LogScanSuccessAsync(
        Guid userId,
        string provider,
        int tokensIn,
        int tokensOut,
        long durationMs,
        string inputHash,
        CancellationToken cancellationToken = default);

    Task LogScanBlockedAsync(
        Guid userId,
        string reason,
        string detail,
        string inputHash,
        CancellationToken cancellationToken = default);

    Task LogScanErrorAsync(
        Guid userId,
        string provider,
        string errorCode,
        long durationMs,
        string inputHash,
        CancellationToken cancellationToken = default);

    private class FakeAiCostCounter : IAiCostCounter
    {
        public int IncrementCallCount { get; private set; }

        public Task IncrementAsync(string providerName, long tokens, CancellationToken cancellationToken = default)
        {
            IncrementCallCount++;
            return Task.CompletedTask;
        }
    }
}