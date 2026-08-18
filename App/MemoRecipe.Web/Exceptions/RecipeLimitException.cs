namespace MemoRecipe.Web.Exceptions;

public class RecipeLimitException : Exception
{
    public int Limit { get; }

    public RecipeLimitException(int limit)
        : base($"Recipe limit reached ({limit})")
    {
        Limit = limit;
    }
}