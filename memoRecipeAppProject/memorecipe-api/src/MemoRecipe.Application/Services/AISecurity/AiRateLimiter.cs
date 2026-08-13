using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Application.Services.AISecurity;

public class AiRateLimiter : IAiRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly AiRateLimitOptions _options;
    private readonly TimeProvider _timeProvider;

    public AiRateLimiter(
        IMemoryCache cache,
        IOptions<AiRateLimitOptions> options,
        TimeProvider timeProvider)
    {
        _cache = cache;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public void CheckAndThrow(string userId, string ipAddress)
    {
        var now = _timeProvider.GetUtcNow();

        var checks = new[]
        {
            new TierCheck(
                $"ai-rate:user:{userId}:hour:{now:yyyyMMddHH}",
                _options.PerUserPerHour, TimeSpan.FromHours(1),
                "per-user-hour", SecondsUntilNextHour(now)),
            new TierCheck(
                $"ai-rate:user:{userId}:day:{now:yyyyMMdd}",
                _options.PerUserPerDay, TimeSpan.FromDays(1),
                "per-user-day", SecondsUntilNextDay(now)),
            new TierCheck(
                $"ai-rate:ip:{ipAddress}:hour:{now:yyyyMMddHH}",
                _options.PerIpPerHour, TimeSpan.FromHours(1),
                "per-ip-hour", SecondsUntilNextHour(now)),
            new TierCheck(
                $"ai-rate:global:minute:{now:yyyyMMddHHmm}",
                _options.GlobalPerMinute, TimeSpan.FromMinutes(1),
                "global-minute", SecondsUntilNextMinute(now)),
        };

        // Pass 1: check all tiers without mutation (fair enforcement)
        foreach (var check in checks)
        {
            var current = _cache.Get<int?>(check.Key) ?? 0;
            if (current >= check.Limit)
            {
                throw new AiRateLimitExceededException(check.TierName, check.RetryAfterSeconds);
            }
        }

        // Pass 2: all checks passed, increment counters
        foreach (var check in checks)
        {
            var current = _cache.Get<int?>(check.Key) ?? 0;
            _cache.Set(check.Key, current + 1,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(check.Window));
        }
    }

    private static int SecondsUntilNextHour(DateTimeOffset now) =>
        3600 - (now.Minute * 60 + now.Second);

    private static int SecondsUntilNextDay(DateTimeOffset now) =>
        86400 - (int)now.TimeOfDay.TotalSeconds;

    private static int SecondsUntilNextMinute(DateTimeOffset now) =>
        60 - now.Second;

    private record TierCheck(
        string Key, int Limit, TimeSpan Window, string TierName, int RetryAfterSeconds);
}