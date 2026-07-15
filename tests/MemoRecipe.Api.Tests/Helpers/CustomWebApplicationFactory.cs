using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MemoRecipe.Infrastructure.Database;
using System.Data.Common;
using MemoRecipe.Application.Services.OcrScan;
using Testcontainers.PostgreSql;

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
    Environment.SetEnvironmentVariable("Telegram__BotToken", "FAKE_TEST_TOKEN_NOT_USED");   // ← ajout
    Environment.SetEnvironmentVariable("Telegram__ChatId", "0");                            // ← ajout
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