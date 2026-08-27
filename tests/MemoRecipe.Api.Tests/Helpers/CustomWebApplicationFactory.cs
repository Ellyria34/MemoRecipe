using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MemoRecipe.Infrastructure.Database;
using System.Data.Common;
using MemoRecipe.Application.Services.OcrScan;
using Testcontainers.PostgreSql;
using Microsoft.Extensions.Configuration;

namespace MemoRecipe.Api.Tests.Helpers;
public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program>, IAsyncLifetime where Program : class
{
static CustomWebApplicationFactory()
{
    Environment.SetEnvironmentVariable("DOTNET_TEST_MODE", "true");
    Environment.SetEnvironmentVariable("JwtSettings__Secret", 
    "TEST_JWT_SECRET_AT_LEAST_64_CHARS_FOR_INTEGRATION_TESTS_PURPOSES_XXX");
    Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", 
    "Host=fake;Database=fake;Username=fake;Password=fake");
    Environment.SetEnvironmentVariable("OcrScan__BaseUrl", "http://fake-ocr/");
    Environment.SetEnvironmentVariable("OcrScan__FunctionKey", "FAKE_TEST_FUNCTION_KEY_NOT_USED");
    Environment.SetEnvironmentVariable("Telegram__BotToken", "FAKE_TEST_TOKEN_NOT_USED");
    Environment.SetEnvironmentVariable("Telegram__ChatId", "0");
    Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:5110");
}


    // Spin up a PostgreSQL container shared across all tests of this class.
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine").Build();

    // Called by xUnit ONCE before all tests start.
    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    // Called by xUnit ONCE after all tests finished.
    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }


    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Test-specific overrides for values that live in appsettings.Development.json
            // locally but are absent on CI runners. This runs AFTER Program.cs config reads,
            // so ONLY suitable for lazy-bound configs (via IOptions<T>). For eager reads
            // (like Cors:AllowedOrigins at Program.cs L48), set env vars in the static ctor.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Features:ScanRecipeEnabled"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove all services related to the PostgreSQL DbContext
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<MemoRecipeDbContext>)
                          || d.ServiceType == typeof(DbContextOptions)
                          || d.ServiceType == typeof(MemoRecipeDbContext)
                          || d.ServiceType == typeof(DbConnection)
                          || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition().FullName?.Contains("IDbContextOptionsConfiguration") == true))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Register a new DbContext that uses the container's connection string.
            services.AddDbContext<MemoRecipeDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            });

            // Build the schema (create tables from EF Core entities)
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            db.Database.Migrate();

            //Build the schema
            // Remove the real IOcrScanService (HTTP call to Azure Function)
            var ocrDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IOcrScanService));
            if (ocrDescriptor != null)
                services.Remove(ocrDescriptor);

            // Register the fake
            services.AddScoped<IOcrScanService, FakeOcrScanService>();
        });

        builder.UseEnvironment("Development");
    }
}