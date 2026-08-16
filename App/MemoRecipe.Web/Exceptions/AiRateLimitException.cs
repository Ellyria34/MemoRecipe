namespace MemoRecipe.Web.Exceptions;

public class AiRateLimitException : Exception
{
    public int RetryAfterSeconds { get; }

    public AiRateLimitException(int retryAfterSeconds)
        : base($"AI rate limit exceeded, retry after {retryAfterSeconds}s")
    {
        RetryAfterSeconds = retryAfterSeconds;
    }
}