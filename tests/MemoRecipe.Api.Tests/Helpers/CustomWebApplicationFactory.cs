using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MemoRecipe.Infrastructure.Database;
using System.Data.Common;

namespace MemoRecipe.Api.Tests.Helpers;


public class CustomWebApplicationFactory<Program> : WebApplicationFactory<Program> where Program : class
{
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

            // Create an in-memory SQLite connection
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            services.AddSingleton<DbConnection>(connection);

            services.AddDbContext<MemoRecipeDbContext>((container, options) =>
            {
                var conn = container.GetRequiredService<DbConnection>();
                options.UseSqlite(conn);
            });

            // Build the schema (create tables from EF Core entities)
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
            db.Database.EnsureCreated();

        });

        builder.UseEnvironment("Development");
    }
}