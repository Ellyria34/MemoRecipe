using MemoRecipe.Application.Services.AISecurity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace MemoRecipe.Application.Tests.Services;

public class AiRateLimiterTests
{
    private static readonly DateTimeOffset FixedTime =
        DateTimeOffset.Parse("2026-08-13T10:30:00Z");

    private static AiRateLimiter CreateLimiter(
        FakeTimeProvider timeProvider,
        AiRateLimitOptions? options = null)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var opts = Options.Create(options ?? new AiRateLimitOptions
        {
            PerUserPerHour = 20,
            PerUserPerDay = 5,
            PerIpPerHour = 30,
            GlobalPerMinute = 20,
        });
        return new AiRateLimiter(cache, opts, timeProvider);
    }

    [Fact]
    public void CheckAndThrow_UnderAllLimits_DoesNotThrow()
    {
        var time = new FakeTimeProvider(FixedTime);
        var limiter = CreateLimiter(time);

        var ex = Record.Exception(() => limiter.CheckAndThrow("user1", "1.2.3.4"));

        Assert.Null(ex);
    }

    [Fact]
    public void CheckAndThrow_ExceedsPerUserHour_ThrowsCorrectTier()
    {
        var time = new FakeTimeProvider(FixedTime);
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 2, PerUserPerDay = 100, PerIpPerHour = 100, GlobalPerMinute = 100,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4"); // 1
        limiter.CheckAndThrow("user1", "1.2.3.4"); // 2

        var ex = Assert.Throws<AiRateLimitExceededException>(
            () => limiter.CheckAndThrow("user1", "1.2.3.4")); // 3 → throw
        Assert.Equal("per-user-hour", ex.Tier);
    }

    [Fact]
    public void CheckAndThrow_ExceedsPerUserDay_ThrowsCorrectTier()
    {
        var time = new FakeTimeProvider(FixedTime);
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 100, PerUserPerDay = 2, PerIpPerHour = 100, GlobalPerMinute = 100,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4");
        limiter.CheckAndThrow("user1", "1.2.3.4");

        var ex = Assert.Throws<AiRateLimitExceededException>(
            () => limiter.CheckAndThrow("user1", "1.2.3.4"));
        Assert.Equal("per-user-day", ex.Tier);
    }

    [Fact]
    public void CheckAndThrow_ExceedsPerIpHour_ThrowsCorrectTier()
    {
        var time = new FakeTimeProvider(FixedTime);
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 100, PerUserPerDay = 100, PerIpPerHour = 2, GlobalPerMinute = 100,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4"); // 1 - même IP, users diff
        limiter.CheckAndThrow("user2", "1.2.3.4"); // 2

        var ex = Assert.Throws<AiRateLimitExceededException>(
            () => limiter.CheckAndThrow("user3", "1.2.3.4"));
        Assert.Equal("per-ip-hour", ex.Tier);
    }

    [Fact]
    public void CheckAndThrow_ExceedsGlobalMinute_ThrowsCorrectTier()
    {
        var time = new FakeTimeProvider(FixedTime);
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 100, PerUserPerDay = 100, PerIpPerHour = 100, GlobalPerMinute = 2,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4"); // users + IPs diff → seul le compteur global grimpe
        limiter.CheckAndThrow("user2", "5.6.7.8");

        var ex = Assert.Throws<AiRateLimitExceededException>(
            () => limiter.CheckAndThrow("user3", "9.9.9.9"));
        Assert.Equal("global-minute", ex.Tier);
    }

    [Fact]
    public void CheckAndThrow_DifferentUsers_HaveIsolatedCounters()
    {
        var time = new FakeTimeProvider(FixedTime);
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 1, PerUserPerDay = 100, PerIpPerHour = 100, GlobalPerMinute = 100,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4"); // user1 rempli

        var ex = Record.Exception(() => limiter.CheckAndThrow("user2", "5.6.7.8"));
        Assert.Null(ex); // user2 non-affecté
    }

    [Fact]
    public void CheckAndThrow_AfterMinuteWindowExpires_ResetsGlobalCounter()
    {
        var time = new FakeTimeProvider(FixedTime);
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 100, PerUserPerDay = 100, PerIpPerHour = 100, GlobalPerMinute = 1,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4"); // rempli

        time.Advance(TimeSpan.FromSeconds(61)); // fenêtre suivante

        var ex = Record.Exception(() => limiter.CheckAndThrow("user1", "1.2.3.4"));
        Assert.Null(ex);
    }

    [Fact]
    public void CheckAndThrow_RetryAfterSeconds_CorrectOnGlobalMinute()
    {
        var time = new FakeTimeProvider(
            DateTimeOffset.Parse("2026-08-13T10:30:15Z")); // 15s dans la minute
        var opts = new AiRateLimitOptions
        {
            PerUserPerHour = 100, PerUserPerDay = 100, PerIpPerHour = 100, GlobalPerMinute = 1,
        };
        var limiter = CreateLimiter(time, opts);

        limiter.CheckAndThrow("user1", "1.2.3.4");
        var ex = Assert.Throws<AiRateLimitExceededException>(
            () => limiter.CheckAndThrow("user2", "5.6.7.8"));

        Assert.Equal("global-minute", ex.Tier);
        Assert.Equal(45, ex.RetryAfterSeconds); // 60 - 15 = 45
    }
}