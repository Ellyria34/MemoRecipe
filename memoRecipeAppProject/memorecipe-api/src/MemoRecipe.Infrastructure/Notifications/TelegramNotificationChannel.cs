using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using MemoRecipe.Application.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoRecipe.Infrastructure.Notifications;

public class TelegramNotificationChannel : INotificationChannel
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TelegramNotificationChannel> _logger;
    private readonly string _botToken;
    private readonly string _chatId;

    public TelegramNotificationChannel(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelegramNotificationChannel> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _botToken = configuration["Telegram:BotToken"]!;
        _chatId = configuration["Telegram:ChatId"]!;
    }

    public async Task SendAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        // 1. Format the text with an emoji prefix based on alert.Level
        var emoji = alert.Level switch
        {
            AlertLevel.Info => "ℹ️",
            AlertLevel.Warning => "⚠️",
            AlertLevel.Critical => "🚨",
            _ => ""
        };
        var notificationAlert = $"{emoji} *{alert.Title}* {alert.Message} _{alert.OccurredAt:yyyy-MM-dd HH:mm:ss}_";

        // 2. Build the JSON payload
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new { chat_id = _chatId, text = notificationAlert, parse_mode = "Markdown" };
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        // 3. POST + log & swallow (a broken Telegram must not crash the API)
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, payload, options, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Telegram API returned {StatusCode}: {Body}",
                    (int)response.StatusCode,
                    Redact(body));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to send alert to Telegram ({ExceptionType}): {Message}",
                ex.GetType().Name,
                Redact(ex.Message));
            // NO rethrow — a broken Telegram must not crash the API
        }
    }

    // Anti-leak helper: masks the bot token if it appears in any string before logging.
    private string Redact(string input)
        => string.IsNullOrEmpty(input) ? input : input.Replace(_botToken, "***");
}
