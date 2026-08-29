using MemoRecipe.Application.Services.Auth;

namespace MemoRecipe.Api.AdminCli;

public static class AdminPasswordResetCommand
{
    public static async Task RunAsync(IServiceProvider services, string[] args)
    {
        var email = GetArgValue(args, "--email")
            ?? throw new InvalidOperationException("--email <address> is required");
        var passwordFile = GetArgValue(args, "--password-file")
            ?? throw new InvalidOperationException("--password-file <path> is required");

        var newPassword = (await File.ReadAllTextAsync(passwordFile)).Trim();
        if (string.IsNullOrEmpty(newPassword))
        {
            throw new InvalidOperationException("Password file is empty");
        }

        using var scope = services.CreateScope();
        var resetService = scope.ServiceProvider
            .GetRequiredService<IAdminPasswordResetService>();

        var result = await resetService.ResetAsync(email, newPassword);

        switch (result)
        {
            case PasswordResetResult.Success:
                Console.WriteLine($"[OK] Password reset succeeded for {email}");
                Environment.Exit(0);
                break;
            case PasswordResetResult.UserNotFound:
                Console.Error.WriteLine($"[ERROR] User not found for email: {email}");
                Environment.Exit(2);
                break;
        }
    }

    private static string? GetArgValue(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length) return null;
        return args[index + 1];
    }
}