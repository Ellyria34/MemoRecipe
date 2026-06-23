using MemoRecipe.Application.Exceptions;
using Microsoft.AspNetCore.Http;

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

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // go to the next step
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
        }
    }
}
