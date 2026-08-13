namespace MemoRecipe.Application.Services.AISecurity;

public interface IAiRateLimiter
{
    void CheckAndThrow(string userId, string ipAddress);
}