namespace MemoRecipe.Application.Services.AISecurity;

public class AiRateLimitExceededException : Exception
{
    public string Tier { get; }
    public int RetryAfterSeconds { get; }

    public AiRateLimitExceededException(string tier, int retryAfterSeconds)
        : base($"AI rate limit exceeded on tier '{tier}'. Retry after {retryAfterSeconds}s.")
    {
        Tier = tier;
        RetryAfterSeconds = retryAfterSeconds;
    }
}