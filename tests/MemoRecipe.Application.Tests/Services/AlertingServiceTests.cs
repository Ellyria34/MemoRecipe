using MemoRecipe.Application.Notifications;
using MemoRecipe.Application.Services.Alerting;
using MemoRecipe.Application.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Application.Tests.Services;

public class AlertingServiceTests
{
    private const int Threshold = 10;

    [Fact]
    public async Task NotifyMassPurgeAsync_WhenBelowThreshold_DoesNotSendAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act
        await sut.NotifyMassPurgeAsync(Threshold - 1);

        // Assert
        Assert.Empty(channel.SentAlerts);
    }

    [Fact]
    public async Task NotifyMassPurgeAsync_WhenAtThresholdExactly_SendsCriticalAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act
        await sut.NotifyMassPurgeAsync(Threshold);

        // Assert — at threshold triggers the alert (>=, not >)
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
    }

    [Fact]
    public async Task NotifyMassPurgeAsync_WhenAboveThreshold_SendsCriticalAlertWithContext()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);
        const int deletedCount = 100;

        // Act
        await sut.NotifyMassPurgeAsync(deletedCount);

        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Mass purge alert", alert.Title);
        Assert.Contains(deletedCount.ToString(), alert.Message);
        Assert.Contains($"threshold: {Threshold}", alert.Message);
    }

    private static AlertingService CreateSut(FakeNotificationChannel channel)
    {
        var options = Options.Create(new AlertingOptions { MassPurgeCritical = Threshold });
        return new AlertingService(channel, options);
    }
}
