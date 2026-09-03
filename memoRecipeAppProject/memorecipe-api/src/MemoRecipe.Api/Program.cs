using MemoRecipe.Infrastructure.Database;
using MemoRecipe.Infrastructure.Repositories;
using MemoRecipe.Application.Repositories;
using MemoRecipe.Application.Services.Recipes;
using Microsoft.EntityFrameworkCore;
using MemoRecipe.Application.Services.Auth;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MemoRecipe.Api.Middlewares;
using FluentValidation;
using MemoRecipe.Application.Validators;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MemoRecipe.Infrastructure.ExternalServices;
using MemoRecipe.Application.Notifications;
using MemoRecipe.Infrastructure.Notifications;
using MemoRecipe.Application.Services.OcrScan;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using MemoRecipe.Application.Services.Alerting;
using MemoRecipe.Application.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using MemoRecipe.Infrastructure.BackgroundServices;
using MemoRecipe.Application.Services.AISecurity;
using MemoRecipe.Application.Services.Monitoring;
using MemoRecipe.Api.AdminCli;
using MemoRecipe.Application.Services.Upload;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyPerFile(
    Environment.GetEnvironmentVariable("SECRETS_PATH") ?? "/run/secrets",
    optional: true);

builder.Services.AddSerilog((services, lc) => lc
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Limite globale Kestrel — empêche les uploads > 15 Mo au niveau transport
// (BACK-041 défense en profondeur, couche 1)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 15 * 1024 * 1024; // 15 Mo
    options.AddServerHeader = false; // OWASP recommendation (limit fingerprinting)
});


// CORS configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
    throw new InvalidOperationException("Cors:AllowedOrigins is not configured in appsettings.json");

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Content-Type")
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowCredentials();
    });
});

RequireConfig(builder.Configuration, "JwtSettings:Secret", "Set the JwtSettings__Secret environment variable in production or update appsettings.Development.json (local dev).");
RequireConfig(builder.Configuration, "ConnectionStrings:DefaultConnection", "Set the ConnectionStrings__DefaultConnection environment variable in production or update appsettings.Development.json (local dev).");
RequireConfig(builder.Configuration, "OcrScan:BaseUrl", "Set the OcrScan__BaseUrl environment variable in production or update appsettings.Development.json (local dev).");
RequireConfig(builder.Configuration, "OcrScan:FunctionKey", "Set the OcrScan__FunctionKey environment variable in production or update appsettings.Development.json (local dev).");
RequireConfig(builder.Configuration, "Telegram:BotToken", "Set the Telegram__BotToken environment variable in production or update appsettings.Development.json (local dev).");
RequireConfig(builder.Configuration, "Telegram:ChatId", "Set the Telegram__ChatId environment variable in production or update appsettings.Development.json (local dev).");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

builder.Services.AddDbContext<MemoRecipeDbContext>(options =>
    options.UseNpgsql(connectionString));

//Authentication service
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JwtBearer";
    options.DefaultChallengeScheme = "JwtBearer";
})
.AddJwtBearer("JwtBearer", options =>
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Secret"]!)
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
       {
           context.Token = context.Request.Cookies["authCookie"];
           return Task.CompletedTask;
       }
    };
});

//Authorization service
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MemoRecipe API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (context, cancellationToken) =>
    {
        // Custom rejection handling logic
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.Headers["Retry-After"] = "60";

        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    // Partitioned by client IP — each IP has its own bucket, so a single attacker cannot
    // exhaust the shared bucket and DoS all other users' logins.
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("scan", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Application services (dependency injection)
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IValidator<RecipeCreateDto>, RecipeCreateDtoValidator>();
builder.Services.AddScoped<IValidator<RecipeUpdateDto>, RecipeUpdateDtoValidator>();
builder.Services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
builder.Services.AddScoped<IValidator<DeleteAccountDto>, DeleteAccountDtoValidator>();
builder.Services.AddHttpClient<IOcrScanService, OcrScanService>();
builder.Services.AddHttpClient<INotificationChannel, TelegramNotificationChannel>();
builder.Services.Configure<AlertingOptions>(
    builder.Configuration.GetSection(AlertingOptions.SectionName));
builder.Services.AddHostedService<AccountPurgeService>();
builder.Services.Configure<FeatureFlagsOptions>(
    builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));
builder.Services.AddScoped<IAlertingService, AlertingService>();
builder.Services.Configure<AccountPurgeOptions>(
    builder.Configuration.GetSection(AccountPurgeOptions.SectionName));
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<IAdminPasswordResetService, AdminPasswordResetService>();
builder.Services.AddScoped<IFileUploadValidator, FileUploadValidator>();
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString);
// AI Security — Rate Limiter (US-A2-04)
builder.Services.Configure<AiRateLimitOptions>(
    builder.Configuration.GetSection(AiRateLimitOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAiRateLimiter, AiRateLimiter>();
builder.Services.AddScoped<IAiAuditLogger, AiAuditLogger>();
builder.Services.Configure<AiCostAlertingOptions>(
    builder.Configuration.GetSection(AiCostAlertingOptions.SectionName));
builder.Services.AddScoped<IAiCostCounter, AiCostCounter>();
builder.Services.Configure<RecipeLimitsOptions>(
    builder.Configuration.GetSection(RecipeLimitsOptions.SectionName));

builder.Services.AddMemoryCache();

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};

forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeadersOptions);

// Auto-apply EF Core migrations on startup.
if (Environment.GetEnvironmentVariable("DOTNET_TEST_MODE") != "true")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        db.Database.Migrate();
    }
}

// Admin CLI mode: --reset-password --email <address> --password-file <path>
// Exits without starting Kestrel (P0-7 runbook).
if (args.Contains("--reset-password"))
{
    await AdminPasswordResetCommand.RunAsync(app.Services, args);
    return;
}


// Configure
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
if (!app.Environment.IsEnvironment("Testing-NoRateLimit"))
{
    app.UseRateLimiter();
}
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();


app.Run();

static void RequireConfig(IConfiguration config, string key, string description)
{
    var configValue = config[key];

    if (string.IsNullOrWhiteSpace(configValue) || configValue.Contains("CHANGE_ME"))
    {
        throw new InvalidOperationException($"Configuration '{key}' is invalid. {description}");
    }
}