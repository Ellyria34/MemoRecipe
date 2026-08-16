using MemoRecipe.Application.Services.Monitoring;
using MemoRecipe.Tests.Shared.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace MemoRecipe.Application.Tests.Services;

public class AiCostCounterTests
{
    private static AiCostCounter CreateCounter(
        FakeAlertingService alerting,
        FakeTimeProvider timeProvider,
        long dailyThreshold = 1000,
        long weeklyThreshold = 5000)
    {
        var options = Options.Create(new AiCostAlertingOptions
        {
            PerProvider = new Dictionary<string, AiCostProviderThresholds>
            {
                ["Mistral"] = new()
                {
                    DailyTokenThreshold = dailyThreshold,
                    WeeklyTokenThreshold = weeklyThreshold
                }
            }
        });

        var cache = new MemoryCache(new MemoryCacheOptions());
        return new AiCostCounter(cache, options, alerting, timeProvider, NullLogger<AiCostCounter>.Instance);
    }

    [Fact]
    public async Task IncrementAsync_WhenDailyThresholdReached_NotifiesDailyOnce()
    {
        var alerting = new FakeAlertingService();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero));
        var counter = CreateCounter(alerting, time, dailyThreshold: 1000, weeklyThreshold: 999999);

        await counter.IncrementAsync("Mistral", 500);
        await counter.IncrementAsync("Mistral", 500); // total = 1000, seuil atteint

        Assert.Equal(1, alerting.AiCostDailyCallCount);
        Assert.Equal(0, alerting.AiCostWeeklyCallCount);
    }

    [Fact]
    public async Task IncrementAsync_WhenWeeklyThresholdReached_NotifiesWeeklyOnce()
    {
        var alerting = new FakeAlertingService();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero));
        var counter = CreateCounter(alerting, time, dailyThreshold: 999999, weeklyThreshold: 5000);

        await counter.IncrementAsync("Mistral", 3000);
        await counter.IncrementAsync("Mistral", 2000); // total = 5000, seuil atteint

        Assert.Equal(1, alerting.AiCostWeeklyCallCount);
        Assert.Equal(0, alerting.AiCostDailyCallCount);
    }

    [Fact]
    public async Task IncrementAsync_WhenDailyThresholdExceededMultipleTimes_NotifiesOnlyOnce()
    {
        var alerting = new FakeAlertingService();
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 8, 16, 10, 0, 0, TimeSpan.Zero));
        var counter = CreateCounter(alerting, time, dailyThreshold: 1000, weeklyThreshold: 999999);

        await counter.IncrementAsync("Mistral", 1000); // seuil atteint → alerte
        await counter.IncrementAsync("Mistral", 500);  // au-dessus → pas de spam
        await counter.IncrementAsync("Mistral", 500);  // encore au-dessus → toujours pas de spam

        Assert.Equal(1, alerting.AiCostDailyCallCount);
    }
}