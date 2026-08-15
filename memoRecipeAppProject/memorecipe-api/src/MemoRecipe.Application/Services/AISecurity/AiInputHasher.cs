using System.Security.Cryptography;
using System.Text;

namespace MemoRecipe.Application.Services.AISecurity;

public static class AiInputHasher
{
    /// <summary>
    /// Deterministic SHA-256 hash of the input, lowercased hex.
    /// Used to trace an audit trail without persisting the raw content (GDPR Art. 5.1.c minimization).
    /// </summary>
    public static string Sha256(string? input)
    {
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}