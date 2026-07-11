namespace MemoRecipe.Application.Helpers;

public static class EmailMasker
{
    private const string FullyMasked = "***";

    public static string Mask(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return FullyMasked;
        }

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
        {
            return FullyMasked;
        }

        var firstChar = email[0];
        var domain = email[atIndex..];
        return $"{firstChar}***{domain}";
    }
}
