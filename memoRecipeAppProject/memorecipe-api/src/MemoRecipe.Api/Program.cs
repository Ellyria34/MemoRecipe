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

var builder = WebApplication.CreateBuilder(args);

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
RequireConfig(builder.Configuration, "Telegram:BotToken", "Set the Telegram__BotToken environment variable in production or update appsettings.Development.json (local dev).");
RequireConfig(builder.Configuration, "Telegram:ChatId", "Set the Telegram__ChatId environment variable in production or update appsettings.Development.json (local dev).");

builder.Services.AddDbContext<MemoRecipeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

        options.AddFixedWindowLimiter("auth", opt =>
        {
            opt.PermitLimit = 10;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });

        options.AddFixedWindowLimiter("scan", opt =>
        {
            opt.PermitLimit = 5;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 0;
        });       
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
builder.Services.AddScoped<IAlertingService, AlertingService>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

var app = builder.Build();

// Auto-apply EF Core migrations on startup.
if (Environment.GetEnvironmentVariable("DOTNET_TEST_MODE") != "true")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MemoRecipeDbContext>();
        db.Database.Migrate();
    }
}

// Configure
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();


app.Run();

static void RequireConfig(IConfiguration config, string key, string description)
{
    var configValue = config[key];

    if(string.IsNullOrWhiteSpace(configValue) || configValue.Contains("CHANGE_ME"))
    {
        throw new InvalidOperationException($"Configuration '{key}' is invalid. {description}");
    }
}
public partial class Program { }
