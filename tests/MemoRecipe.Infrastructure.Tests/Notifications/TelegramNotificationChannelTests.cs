using System.Net;
using System.Text.Json;
using MemoRecipe.Application.Notifications;
using MemoRecipe.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoRecipe.Infrastructure.Tests.Notifications;

public class TelegramNotificationChannelTests
{
    [Fact]
    public async Task SendAsync_BuildsCorrectRequest()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var handler = new FakeHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok":true}""")
            };
        });
        var sut = CreateSut(handler);

        // Act
        await sut.SendAsync(new Alert(AlertLevel.Warning, "Test", "Message", DateTimeOffset.UtcNow));

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Contains("api.telegram.org/bot", capturedRequest.RequestUri!.ToString());
        Assert.Contains("/sendMessage", capturedRequest.RequestUri.ToString());

        Assert.Contains("\"chat_id\"", capturedBody);
        Assert.Contains("\"text\"", capturedBody);
        Assert.Contains("\"parse_mode\":\"Markdown\"", capturedBody);

        var text = ExtractText(capturedBody);
        Assert.Contains("⚠️", text);
        Assert.Contains("Test", text);
    }

    [Fact]
    public async Task SendAsync_WithCriticalLevel_UsesRotatingLightEmoji()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async request =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CreateSut(handler);

        // Act
        await sut.SendAsync(new Alert(AlertLevel.Critical, "Boom", "Msg", DateTimeOffset.UtcNow));

        // Assert
        var text = ExtractText(capturedBody);
        Assert.Contains("🚨", text);
        Assert.DoesNotContain("ℹ️", text);
        Assert.DoesNotContain("⚠️", text);
    }

    [Fact]
    public async Task SendAsync_WithInfoLevel_UsesInfoEmoji()
    {
        // Arrange
        string? capturedBody = null;
        var handler = new FakeHttpMessageHandler(async request =>
        {
            capturedBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = CreateSut(handler);

        // Act
        await sut.SendAsync(new Alert(AlertLevel.Info, "Note", "Msg", DateTimeOffset.UtcNow));

        // Assert
        var text = ExtractText(capturedBody);
        Assert.Contains("ℹ️", text);
        Assert.DoesNotContain("🚨", text);
        Assert.DoesNotContain("⚠️", text);
    }

    [Fact]
    public async Task SendAsync_WhenTelegramReturns500_LogsErrorAndDoesNotThrow()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("Server exploded")
            }));
        var (sut, logger) = CreateSutWithLogger(handler);

        // Act — must NOT throw
        var exception = await Record.ExceptionAsync(() =>
            sut.SendAsync(new Alert(AlertLevel.Warning, "T", "M", DateTimeOffset.UtcNow)));

        // Assert
        Assert.Null(exception);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        // Anti-leak double-check: bot token must never appear in any log
        Assert.All(logger.Entries, e => Assert.DoesNotContain("fake-token", e.Message));
    }

    [Fact]
    public async Task SendAsync_WhenHttpClientThrows_LogsErrorAndDoesNotThrow()
    {
        // Arrange — simulate an HttpRequestException whose message contains the bot token
        // (this is what a real .NET HttpRequestException does when the URL contains the token)
        var handler = new FakeHttpMessageHandler(_ =>
            throw new HttpRequestException(
                "Network down (https://api.telegram.org/botfake-token/sendMessage)"));
        var (sut, logger) = CreateSutWithLogger(handler);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            sut.SendAsync(new Alert(AlertLevel.Warning, "T", "M", DateTimeOffset.UtcNow)));

        // Assert
        Assert.Null(exception);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error);
        // Anti-leak double-check: token from the exception message must be redacted
        Assert.All(logger.Entries, e => Assert.DoesNotContain("fake-token", e.Message));
    }

    // ---------- Helpers ----------

    private static TelegramNotificationChannel CreateSut(HttpMessageHandler handler)
        => CreateSutWithLogger(handler).Sut;

    private static (TelegramNotificationChannel Sut, FakeLogger<TelegramNotificationChannel> Logger)
        CreateSutWithLogger(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Telegram:BotToken"] = "fake-token",
                ["Telegram:ChatId"] = "123456789"
            })
            .Build();
        var logger = new FakeLogger<TelegramNotificationChannel>();
        return (new TelegramNotificationChannel(httpClient, config, logger), logger);
    }

    private static string ExtractText(string? body)
    {
        using var doc = JsonDocument.Parse(body!);
        return doc.RootElement.GetProperty("text").GetString()!;
    }
}
