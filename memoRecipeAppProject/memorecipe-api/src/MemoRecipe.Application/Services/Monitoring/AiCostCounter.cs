using MemoRecipe.Application.Services.Alerting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Application.Services.Monitoring;

public class AiCostCounter : IAiCostCounter
{
    private readonly IMemoryCache _cache;
    private readonly AiCostAlertingOptions _options;
    private readonly IAlertingService _alerting;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AiCostCounter> _logger;

    public AiCostCounter(
        IMemoryCache cache,
        IOptions<AiCostAlertingOptions> options,
        IAlertingService alerting,
        TimeProvider timeProvider,
        ILogger<AiCostCounter> logger)
    {
        _cache = cache;
        _options = options.Value;
        _alerting = alerting;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task IncrementAsync(string providerName, long tokens, CancellationToken cancellationToken = default)
    {
        // Skip if no provider info or LLM error path
        if (string.IsNullOrEmpty(providerName) || providerName == "unknown")
        {
            return;
        }

        // Skip if no thresholds configured for this provider
        if (!_options.PerProvider.TryGetValue(providerName, out var thresholds))
        {
            _logger.LogWarning(
                "No AI cost thresholds configured for provider {Provider}, skipping counter",
                providerName);
            return;
        }

        var now = _timeProvider.GetUtcNow();

        // Daily counter (resets at UTC midnight)
        var dailyKey = $"cost:daily:{providerName}:{now:yyyy-MM-dd}";
        var dailyCount = _cache.Get<long>(dailyKey) + tokens;
        _cache.Set(dailyKey, dailyCount, EndOfDayUtc(now));

        if (dailyCount >= thresholds.DailyTokenThreshold)
        {
            var notifiedKey = $"cost:daily-notified:{providerName}:{now:yyyy-MM-dd}";
            if (!_cache.TryGetValue(notifiedKey, out _))
            {
                _cache.Set(notifiedKey, true, EndOfDayUtc(now));
                await _alerting.NotifyAiCostDailyAsync(
                    providerName, dailyCount, thresholds.DailyTokenThreshold, cancellationToken);
            }
        }

        // Weekly counter (resets Sunday 23:59:59 UTC)
        var weeklyKey = $"cost:weekly:{providerName}:{WeekKey(now)}";
        var weeklyCount = _cache.Get<long>(weeklyKey) + tokens;
        _cache.Set(weeklyKey, weeklyCount, EndOfWeekUtc(now));

        if (weeklyCount >= thresholds.WeeklyTokenThreshold)
        {
            var notifiedKey = $"cost:weekly-notified:{providerName}:{WeekKey(now)}";
            if (!_cache.TryGetValue(notifiedKey, out _))
            {
                _cache.Set(notifiedKey, true, EndOfWeekUtc(now));
                await _alerting.NotifyAiCostWeeklyAsync(
                    providerName, weeklyCount, thresholds.WeeklyTokenThreshold, cancellationToken);
            }
        }
    }

    private static DateTimeOffset EndOfDayUtc(DateTimeOffset now)
        => new(now.Year, now.Month, now.Day, 23, 59, 59, 999, TimeSpan.Zero);

    private static DateTimeOffset EndOfWeekUtc(DateTimeOffset now)
    {
        // Days remaining until Sunday (Sunday=0 in DayOfWeek enum)
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        var sunday = now.Date.AddDays(daysUntilSunday);
        return new DateTimeOffset(sunday.Year, sunday.Month, sunday.Day, 23, 59, 59, 999, TimeSpan.Zero);
    }

    private static string WeekKey(DateTimeOffset now)
    {
        var week = System.Globalization.ISOWeek.GetWeekOfYear(now.DateTime);
        return $"{now.Year}-W{week:D2}";
    }
}