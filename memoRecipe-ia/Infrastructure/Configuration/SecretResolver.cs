namespace MemoRecipeIA.Infrastructure.Configuration;

/// <summary>
/// Resolves a secret from an inline value or from a file (Docker secrets pattern).
/// </summary>
public static class SecretResolver
{
    /// <summary>
    /// Returns the inline value when set, otherwise the trimmed content of the file
    /// at <paramref name="filePath"/>, otherwise null.
    /// </summary>
    public static string? Resolve(string? value, string? filePath)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            var fromFile = File.ReadAllText(filePath).Trim();
            return string.IsNullOrWhiteSpace(fromFile) ? null : fromFile;
        }

        return null;
    }
}

