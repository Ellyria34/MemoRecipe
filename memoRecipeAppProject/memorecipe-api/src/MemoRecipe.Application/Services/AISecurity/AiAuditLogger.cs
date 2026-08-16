using Microsoft.Extensions.Logging;
using MemoRecipe.Application.Services.Monitoring;

namespace MemoRecipe.Application.Services.AISecurity;

public class AiAuditLogger : IAiAuditLogger
{
    private readonly ILogger<AiAuditLogger> _logger;
    private readonly IAiCostCounter _costCounter;

    public AiAuditLogger(ILogger<AiAuditLogger> logger, IAiCostCounter costCounter)
    {
        _logger = logger;
        _costCounter = costCounter;

    }

    public async Task LogScanSuccessAsync(
        Guid userId,
        string provider,
        int tokensIn,
        int tokensOut,
        long durationMs,
        string inputHash,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "AiScanSuccess UserId={UserId} Provider={Provider} TokensIn={TokensIn} TokensOut={TokensOut} DurationMs={DurationMs} InputHash={InputHash}",
            userId, provider, tokensIn, tokensOut, durationMs, inputHash);

        await _costCounter.IncrementAsync(provider, tokensIn + tokensOut, cancellationToken);
    }

    public Task LogScanBlockedAsync(
        Guid userId,
        string reason,
        string detail,
        string inputHash,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "AiScanBlocked UserId={UserId} Reason={Reason} Detail={Detail} InputHash={InputHash}",
            userId, reason, detail, inputHash);
        return Task.CompletedTask;
    }

    public Task LogScanErrorAsync(
        Guid userId,
        string provider,
        string errorCode,
        long durationMs,
        string inputHash,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(
            "AiScanError UserId={UserId} Provider={Provider} ErrorCode={ErrorCode} DurationMs={DurationMs} InputHash={InputHash}",
            userId, provider, errorCode, durationMs, inputHash);
        return Task.CompletedTask;
    }
}