using MemoRecipe.Api.Middlewares;
using MemoRecipe.Tests.Shared.Fakes;
using MemoRecipe.Application.Exceptions;
using MemoRecipe.Application.Services.AISecurity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace MemoRecipe.Api.Tests.MiddleWares;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenNextThrows_TriggersServerErrorAlertAndReturns500()
    {
        // Arrange
        var alertingService = new FakeAlertingService();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, alertingService);

        // Assert
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(1, alertingService.ServerErrorCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenHealthEndpointThrows_DoesNotTriggerAlert()
    {
        // Arrange
        var alertingService = new FakeAlertingService();
        RequestDelegate next = _ => throw new InvalidOperationException("boom");
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/health";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, alertingService);

        // Assert — /health path is filtered out to avoid alert spam
        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal(0, alertingService.ServerErrorCallCount);
    }

    [Fact]
    public async Task InvokeAsync_WhenAccountMarkedForDeletion_DoesNotTriggerAlert()
    {
        // Arrange
        var alertingService = new FakeAlertingService();
        RequestDelegate next = _ => throw new AccountMarkedForDeletionException();
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, alertingService);

        // Assert — 403 is a business path, not an incident
        Assert.Equal(403, context.Response.StatusCode);
        Assert.Equal(0, alertingService.ServerErrorCallCount);
    }

    [Theory]
    [InlineData("per-user-hour", 3600)]
    [InlineData("per-user-day", 86400)]
    [InlineData("per-ip-hour", 3600)]
    [InlineData("global-minute", 60)]
    public async Task InvokeAsync_WhenAiRateLimitExceeded_Returns429WithRetryAfterHeader(
    string tier, int retryAfterSeconds)
    {
        // Arrange
        var alertingService = new FakeAlertingService();
        RequestDelegate next = _ => throw new AiRateLimitExceededException(tier, retryAfterSeconds);
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context, alertingService);

        // Assert — 429 is a rate-limit business path, not an incident
        Assert.Equal(429, context.Response.StatusCode);
        Assert.Equal(retryAfterSeconds.ToString(), context.Response.Headers["Retry-After"].ToString());
        Assert.Equal(0, alertingService.ServerErrorCallCount);
    }
}
