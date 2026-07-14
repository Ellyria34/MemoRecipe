using MemoRecipe.Application.Notifications;
using MemoRecipe.Application.Services.Alerting;
using MemoRecipe.Application.Tests.Fakes;
using MemoRecipe.Application.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace MemoRecipe.Application.Tests.Services;

public class AlertingServiceTests
{
    private const int MassPurgeThreshold = 10;
    private const int LoginFailThreshold = 5;
    private const int ServerErrorSpikeThreshold = 5;

    #region Mass Purge Alert
    [Fact]
    public async Task NotifyMassPurgeAsync_WhenBelowThreshold_DoesNotSendAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act
        await sut.NotifyMassPurgeAsync(MassPurgeThreshold - 1);

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
        await sut.NotifyMassPurgeAsync(MassPurgeThreshold);

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
        Assert.Contains($"threshold: {MassPurgeThreshold}", alert.Message);
    }
    #endregion

    #region Login fail
    [Fact]
    public async Task NotifyLoginFailAsync_WhenBelowThreshold_DoesNotSendAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act — 4 fails (seuil = 5)
        for (var i = 0; i < LoginFailThreshold - 1; i++)
        {
            await sut.NotifyLoginFailAsync();
        }

        // Assert
        Assert.Empty(channel.SentAlerts);
    }

    [Fact]
    public async Task NotifyLoginFailAsync_WhenReachingThresholdExactly_SendsCriticalAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act — 5 fails (seuil = 5)
        for (var i = 0; i < LoginFailThreshold; i++)
        {
            await sut.NotifyLoginFailAsync();
        }

        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Login fail storm detected", alert.Title);
        Assert.Contains(LoginFailThreshold.ToString(), alert.Message);
        Assert.Contains("5 min", alert.Message);
    }

    [Fact]
    public async Task NotifyLoginFailAsync_WhenAboveThreshold_DoesNotSpam()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act — 7 fails (seuil = 5)
        for (var i = 0; i < LoginFailThreshold + 2; i++)
        {
            await sut.NotifyLoginFailAsync();
        }
        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Login fail storm detected", alert.Title);
        Assert.Contains(LoginFailThreshold.ToString(), alert.Message);
        Assert.Contains("5 min", alert.Message);
    }
    #endregion

    #region Server Error
    [Fact]
    public async Task NotifyServerErrorAsync_WhenBelowThreshold_DoesNotSendAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act — 4 errors (seuil = 5)
        for (var i = 0; i < ServerErrorSpikeThreshold - 1; i++)
        {
            await sut.NotifyServerErrorAsync();
        }

        // Assert
        Assert.Empty(channel.SentAlerts);
    }

    [Fact]
    public async Task NotifyServerErrorAsync_WhenReachingThresholdExactly_SendsCriticalAlert()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act — 4 errors (seuil = 5)
        for (var i = 0; i < ServerErrorSpikeThreshold; i++)
        {
            await sut.NotifyServerErrorAsync();
        }

        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Server error spike detected", alert.Title);
        Assert.Contains(ServerErrorSpikeThreshold.ToString(), alert.Message);
        Assert.Contains("5 min", alert.Message);
    }
    [Fact]
    public async Task NotifyServerErrorAsync_WhenAboveThreshold_DoesNotSpam()
    {
        // Arrange
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel);

        // Act — 7 errors (seuil = 5)
        for (var i = 0; i < ServerErrorSpikeThreshold + 2; i++)
        {
            await sut.NotifyServerErrorAsync();
        }

        // Assert — still only 1 alert despite 7 calls
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Server error spike detected", alert.Title);
        Assert.Contains(ServerErrorSpikeThreshold.ToString(), alert.Message);
        Assert.Contains("5 min", alert.Message);
    }
    #endregion

    #region Backup Stale
    [Fact]
    public async Task NotifyBackupStaleAsync_WhenRecentBackupExists_DoesNotSendAlert()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var backupFile = Path.Combine(tempDir.Path, "backup.gpg");
        File.WriteAllText(backupFile, "fake");

        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel, tempDir.Path);

        // Act
        await sut.NotifyBackupStaleAsync();

        // Assert
        Assert.Empty(channel.SentAlerts);
    }

    [Fact]
    public async Task NotifyBackupStaleAsync_WhenOldBackupExists_SendsCriticalAlert()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var backupFile = Path.Combine(tempDir.Path, "backup.gpg");
        File.WriteAllText(backupFile, "fake");
        File.SetLastWriteTimeUtc(backupFile, DateTime.UtcNow.AddHours(-30));

        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel, tempDir.Path);

        // Act
        await sut.NotifyBackupStaleAsync();

        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Backup stale", alert.Title);
        Assert.Contains("30h old", alert.Message);
        Assert.Contains("threshold: 26h", alert.Message);

    }

    [Fact]
    public async Task NotifyBackupStaleAsync_WhenBackupEmptyFolder_SendsCriticalAlert()
    {
        // Arrange
        using var tempDir = new TempDirectory();

        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel, tempDir.Path);

        // Act
        await sut.NotifyBackupStaleAsync();

        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Backup stale", alert.Title);
        Assert.Contains("No backup file found", alert.Message);
    }

    [Fact]
    public async Task NotifyBackupStaleAsync_WhenBackupFolderNotExist_SendsCriticalAlert()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var channel = new FakeNotificationChannel();
        var sut = CreateSut(channel, nonExistentPath);

        // Act
        await sut.NotifyBackupStaleAsync();

        // Assert
        var alert = Assert.Single(channel.SentAlerts);
        Assert.Equal(AlertLevel.Critical, alert.Level);
        Assert.Equal("Backup stale", alert.Title);
        Assert.Contains("No backup file found", alert.Message);
    }
    #endregion

    private static AlertingService CreateSut(FakeNotificationChannel channel, string? backupPath = null)
    {
        var options = Options.Create(new AlertingOptions
        {
            MassPurgeCritical = MassPurgeThreshold,
            LoginFailStormCritical = LoginFailThreshold,
            ServerErrorSpikeCritical = ServerErrorSpikeThreshold,
            BackupPath = backupPath ?? "/backups"
        });
        var cache = new MemoryCache(new MemoryCacheOptions());

        return new AlertingService(channel, options, cache);
    }
}
