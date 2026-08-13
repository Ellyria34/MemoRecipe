using Microsoft.Extensions.Logging;

namespace MemoRecipe.Application.Services.AISecurity;

public class AiAuditLogger : IAiAuditLogger
{
    private readonly ILogger<AiAuditLogger> _logger;

    public AiAuditLogger(ILogger<AiAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogScanSuccess(
        Guid userId,
        string provider,
        int tokensIn,
        int tokensOut,
        long durationMs,
        string inputHash)
    {
        _logger.LogInformation(
            "AiScanSuccess UserId={UserId} Provider={Provider} TokensIn={TokensIn} TokensOut={TokensOut} DurationMs={DurationMs} InputHash={InputHash}",
            userId, provider, tokensIn, tokensOut, durationMs, inputHash);
    }

    public void LogScanBlocked(
        Guid userId,
        string reason,
        string detail,
        string inputHash)
    {
        _logger.LogWarning(
            "AiScanBlocked UserId={UserId} Reason={Reason} Detail={Detail} InputHash={InputHash}",
            userId, reason, detail, inputHash);
    }

    public void LogScanError(
        Guid userId,
        string provider,
        string errorCode,
        long durationMs,
        string inputHash)
    {
        _logger.LogError(
            "AiScanError UserId={UserId} Provider={Provider} ErrorCode={ErrorCode} DurationMs={DurationMs} InputHash={InputHash}",
            userId, provider, errorCode, durationMs, inputHash);
    }
}