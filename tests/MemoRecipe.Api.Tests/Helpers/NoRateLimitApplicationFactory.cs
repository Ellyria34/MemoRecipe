using Microsoft.AspNetCore.Hosting;

namespace MemoRecipe.Api.Tests.Helpers;

// Test factory that skips the rate limiter middleware (see Program.cs).
// Used by test classes that need to hit auth endpoints multiple times.
// Other tests (including RateLimitingTests which verify the limiter fires)
// keep using the base CustomWebApplicationFactory.
public class NoRateLimitApplicationFactory<TProgram> : CustomWebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Testing-NoRateLimit");
    }
}
