using Microsoft.AspNetCore.Identity;
using MemoRecipe.Domain.Entities.Users;
using System.Security.Cryptography;
using System.Text;

namespace MemoRecipe.Application.Services.Auth;

public class PasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();
    public string HashPassword(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool Verify(User user, string hashedPassword, string password, string? salt)
    {
        // 1. New Format (PasswordHasher<T>)
        var result = _hasher.VerifyHashedPassword(user, hashedPassword, password);
        if (result == PasswordVerificationResult.Success 
            || result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            return true;
        }

        // If the new one fails and there's a salt, try the old HMAC-SHA512
        if (!string.IsNullOrEmpty(salt))
        {
            return VerifyLegacy(password, hashedPassword, salt);
        }

        return false;
    }

    public bool NeedsRehash(string? salt)
    {
        return !string.IsNullOrEmpty(salt);
    }

    // TODO: Old system — to be removed once all users have migrated
    private static bool VerifyLegacy(string password, string hash, string salt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(salt));
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(computed) == hash;
    }

}
