namespace MemoRecipe.Application.Services.AISecurity;

public interface IAiAuditLogger
{
    void LogScanSuccess(
        Guid userId,
        string provider,
        int tokensIn,
        int tokensOut,
        long durationMs,
        string inputHash);

    void LogScanBlocked(
        Guid userId,
        string reason,
        string detail,
        string inputHash);

    void LogScanError(
        Guid userId,
        string provider,
        string errorCode,
        long durationMs,
        string inputHash);
}