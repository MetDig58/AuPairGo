using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationRetryTimingTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationRetryTimingTests(
        TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Retry_worker_does_not_retry_a_recent_failure()
    {
        // Arrange: create users.
        var recipient = new User
        {
            Email = "retry-timing-recipient@test.com",
            FirstName = "Retry Timing",
            LastName = "Recipient",
            CellNumber = "0000000380",
            Role = "Parent",
            OneSignalPlayerId = "retry-timing-recipient-device"
        };

        var sender = new User
        {
            Email = "retry-timing-sender@test.com",
            FirstName = "Retry Timing",
            LastName = "Sender",
            CellNumber = "0000000381",
            Role = "AuPair",
            OneSignalPlayerId = "retry-timing-sender-device"
        };

        _fixture.Context.Users.AddRange(recipient, sender);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a failed notification from only a few seconds ago.
        var recentAttempt = DateTime.UtcNow.AddSeconds(-10);

        var notification = new NotificationLog
        {
            SenderUserId = sender.Id,
            RecipientUserId = recipient.Id,
            Title = "Recent retry notification",
            Message = "This failure is too recent to retry.",
            Type = NotificationType.General,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Failed,
            PushAttemptCount = 1,
            PushError = "Recent failure.",
            PushLastAttemptAt = recentAttempt,
            CreatedAt = recentAttempt
        };

        _fixture.Context.NotificationLogs.Add(notification);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        // Arrange: provide the worker dependencies.
        var services = new ServiceCollection();

        services.AddSingleton<ApplicationDbContext>(_fixture.Context);
        services.AddSingleton<IOneSignalService>(fakeOneSignal);

        using var serviceProvider = services.BuildServiceProvider();

        var worker = new NotificationRetryWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationRetryWorker>.Instance);

        using var cancellationSource = new CancellationTokenSource();

        // Act: start the retry worker.
        await worker.StartAsync(cancellationSource.Token);

        await Task.Delay(250);

        cancellationSource.Cancel();

        await worker.StopAsync(CancellationToken.None);

        // Assert: the recent notification was not retried.
        Assert.Empty(fakeOneSignal.Attempts);

        var savedNotification =
            await _fixture.Context.NotificationLogs.FindAsync(
                notification.Id);

        Assert.NotNull(savedNotification);
        Assert.Equal(
            PushDeliveryStatus.Failed,
            savedNotification.PushDeliveryStatus);

        Assert.Equal(1, savedNotification.PushAttemptCount);
        Assert.Equal(
            "Recent failure.",
            savedNotification.PushError);
    }
}