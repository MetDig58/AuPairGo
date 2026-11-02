using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationRetryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationRetryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Retry_worker_retries_an_old_failed_notification()
    {
        // Arrange: create a recipient with a registered device.
        var recipient = new User
        {
            Email = "retry-recipient@test.com",
            FirstName = "Retry",
            LastName = "Recipient",
            CellNumber = "0000000360",
            Role = "Parent",
            OneSignalPlayerId = "retry-recipient-device"
        };

        var sender = new User
        {
            Email = "retry-sender@test.com",
            FirstName = "Retry",
            LastName = "Sender",
            CellNumber = "0000000361",
            Role = "AuPair",
            OneSignalPlayerId = "retry-sender-device"
        };

        _fixture.Context.Users.AddRange(recipient, sender);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a failed notification old enough to retry.
        var notification = new NotificationLog
        {
            SenderUserId = sender.Id,
            RecipientUserId = recipient.Id,
            Title = "Retry notification",
            Message = "This notification should be retried.",
            Type = NotificationType.General,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Failed,
            PushAttemptCount = 1,
            PushError = "Previous delivery failed.",
            PushLastAttemptAt = DateTime.UtcNow.AddMinutes(-2),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _fixture.Context.NotificationLogs.Add(notification);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        // Arrange: register the real database context and fake push service
        // so the worker can resolve them from a dependency-injection scope.
        var services = new ServiceCollection();

        services.AddSingleton<ApplicationDbContext>(_fixture.Context);
        services.AddSingleton<IOneSignalService>(fakeOneSignal);

        var serviceProvider = services.BuildServiceProvider();

        var worker = new NotificationRetryWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationRetryWorker>.Instance);

        using var cancellationSource = new CancellationTokenSource();

        // Act: start the worker. It performs one retry cycle immediately,
        // then waits for its next cycle.
        await worker.StartAsync(cancellationSource.Token);

        // Give the background worker a short opportunity to process the retry.
        await Task.Delay(250);

        cancellationSource.Cancel();

        await worker.StopAsync(CancellationToken.None);

        // Assert: exactly one retry push was recorded.
        var pushAttempt = Assert.Single(fakeOneSignal.Attempts);

        Assert.Equal(
            "retry-recipient-device",
            pushAttempt.SubscriptionId);

        Assert.Equal(
            notification.Id,
            pushAttempt.NotificationId);

        // Assert: the notification was marked as successfully delivered.
        var savedNotification =
            await _fixture.Context.NotificationLogs.FindAsync(
                notification.Id);

        Assert.NotNull(savedNotification);
        Assert.Equal(
            PushDeliveryStatus.Sent,
            savedNotification.PushDeliveryStatus);

        Assert.Equal(2, savedNotification.PushAttemptCount);
        Assert.Null(savedNotification.PushError);
        Assert.NotNull(savedNotification.PushSentAt);
    }
}