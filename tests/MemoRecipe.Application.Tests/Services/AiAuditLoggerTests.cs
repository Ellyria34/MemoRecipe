using MemoRecipe.Application.Services.AISecurity;
using MemoRecipe.Application.Services.Monitoring;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace MemoRecipe.Application.Tests.Services;

public class AiAuditLoggerTests
{
    // ===== AiInputHasher =====

    [Fact]
    public void Sha256_SameInput_ReturnsSameHash()
    {
        var h1 = AiInputHasher.Sha256("recette de poulet au curry");
        var h2 = AiInputHasher.Sha256("recette de poulet au curry");

        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Sha256_DifferentInputs_ReturnsDifferentHashes()
    {
        var h1 = AiInputHasher.Sha256("recette A");
        var h2 = AiInputHasher.Sha256("recette B");

        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void Sha256_NullOrEmpty_ReturnsHashOfEmptyString()
    {
        var expectedForEmpty = AiInputHasher.Sha256("");

        Assert.Equal(expectedForEmpty, AiInputHasher.Sha256(null));
        Assert.Equal(64, expectedForEmpty.Length); // SHA-256 = 64 hex chars
    }

    // ===== AiAuditLogger =====

    [Fact]
    public async Task LogScanSuccessAsync_EmitsInformationWithStructuredProperties()
    {
        var logger = new FakeLogger<AiAuditLogger>();
        var costCounter = new FakeAiCostCounter();
        var auditLogger = new AiAuditLogger(logger, costCounter);
        var userId = Guid.NewGuid();

        await auditLogger.LogScanSuccessAsync(userId, "MistralVision", 1200, 350, 4500, "abc123");

        var entry = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "UserId" && kv.Value == userId.ToString());
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "Provider" && kv.Value == "MistralVision");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "TokensIn" && kv.Value == "1200");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "TokensOut" && kv.Value == "350");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "DurationMs" && kv.Value == "4500");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "InputHash" && kv.Value == "abc123");
    }

    [Fact]
    public async Task LogScanBlockedAsync_EmitsWarningWithStructuredProperties()
    {
        var logger = new FakeLogger<AiAuditLogger>();
        var costCounter = new FakeAiCostCounter();
        var auditLogger = new AiAuditLogger(logger, costCounter);
        var userId = Guid.NewGuid();

        await auditLogger.LogScanBlockedAsync(userId, "rate-limit", "per-user-hour", "abc123");

        var entry = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "UserId" && kv.Value == userId.ToString());
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "Reason" && kv.Value == "rate-limit");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "Detail" && kv.Value == "per-user-hour");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "InputHash" && kv.Value == "abc123");
    }

    [Fact]
    public async Task LogScanErrorAsync_EmitsErrorWithStructuredProperties()
    {
        var logger = new FakeLogger<AiAuditLogger>();
        var costCounter = new FakeAiCostCounter();
        var auditLogger = new AiAuditLogger(logger, costCounter);
        var userId = Guid.NewGuid();

        await auditLogger.LogScanErrorAsync(userId, "MistralVision", "429", 200, "abc123");

        var entry = Assert.Single(logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "UserId" && kv.Value == userId.ToString());
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "Provider" && kv.Value == "MistralVision");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "ErrorCode" && kv.Value == "429");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "DurationMs" && kv.Value == "200");
        Assert.Contains(entry.StructuredState!, kv => kv.Key == "InputHash" && kv.Value == "abc123");
    }

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
