using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace MemoRecipe.Api.Tests.Helpers;

// Test factory that overrides RecipeLimits:MaxPerUser to 2 to test quota enforcement
// without seeding 200 recipes. Other tests keep the base 200 default.
public class LowQuotaWebApplicationFactory<TProgram> : CustomWebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RecipeLimits:MaxPerUser"] = "2"
            });
        });
    }
}
