namespace MemoRecipe.Application.Services.Monitoring;

public interface IAiCostCounter
{
    Task IncrementAsync(string providerName, long tokens, CancellationToken cancellationToken = default);
}