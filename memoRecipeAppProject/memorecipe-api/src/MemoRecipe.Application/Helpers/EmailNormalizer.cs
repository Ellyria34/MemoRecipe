namespace MemoRecipe.Application.Helpers;

public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }
}