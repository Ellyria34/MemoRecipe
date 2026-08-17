namespace MemoRecipe.Application.Exceptions;

public class RecipeLimitReachedException : Exception
{
    public int Limit { get; }

    public RecipeLimitReachedException(int limit)
        : base($"Recipe limit reached ({limit}). Delete some recipes to create new ones.")
    {
        Limit = limit;
    }
}