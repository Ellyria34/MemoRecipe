using System.Security.Cryptography;
using System.Text;

namespace MemoRecipe.Application.Services.Auth;

public static class PasswordHasher
{
    public static void CreateHash(string password, out string hash, out string salt)
    {
        using var hmac = new HMACSHA512();
        salt = Convert.ToBase64String(hmac.Key);
        hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public static bool Verify(string password, string hash, string salt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(salt));
        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(computed) == hash;
    }
}
