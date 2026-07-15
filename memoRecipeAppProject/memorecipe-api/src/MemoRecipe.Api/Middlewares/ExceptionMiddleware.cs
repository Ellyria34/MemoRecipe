using MemoRecipe.Application.Exceptions;
using MemoRecipe.Application.Services.Alerting;

namespace MemoRecipe.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // Note: IAlertingService is injected per-request via method parameter
    // because it's scoped and this middleware is singleton.
    public async Task InvokeAsync(HttpContext context, IAlertingService alertingService)
    {
        try
        {
            await _next(context);
        }
        catch (AccountMarkedForDeletionException ex)
        {
            _logger.LogWarning(ex, "Write attempt blocked: account marked for deletion");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { status = 403, title = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(new { status = 500, title = "An unexpected error occurred." });

            // Skip alerting for /health failures — they surface via the healthcheck itself
            if (!context.Request.Path.StartsWithSegments("/health"))
            {
                await alertingService.NotifyServerErrorAsync();
            }
        }
    }
}
