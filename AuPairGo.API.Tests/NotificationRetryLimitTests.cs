using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationRetryLimitTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationRetryLimitTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Retry_worker_does_not_retry_after_five_attempts()
    {
        // Arrange: create a recipient with a registered device.
        var recipient = new User
        {
            Email = "retry-limit-recipient@test.com",
            FirstName = "Retry Limit",
            LastName = "Recipient",
            CellNumber = "0000000370",
            Role = "Parent",
            OneSignalPlayerId = "retry-limit-recipient-device"
        };

        var sender = new User
        {
            Email = "retry-limit-sender@test.com",
            FirstName = "Retry Limit",
            LastName = "Sender",
            CellNumber = "0000000371",
            Role = "AuPair",
            OneSignalPlayerId = "retry-limit-sender-device"
        };

        _fixture.Context.Users.AddRange(recipient, sender);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a failed notification that has reached the limit.
        var notification = new NotificationLog
        {
            SenderUserId = sender.Id,
            RecipientUserId = recipient.Id,
            Title = "Retry limit notification",
            Message = "This notification must not be retried again.",
            Type = NotificationType.General,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Failed,
            PushAttemptCount = 5,
            PushError = "Maximum attempts already reached.",
            PushLastAttemptAt = DateTime.UtcNow.AddMinutes(-2),
            CreatedAt = DateTime.UtcNow.AddMinutes(-2)
        };

        _fixture.Context.NotificationLogs.Add(notification);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        // Arrange: provide the worker's dependencies through DI.
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

        // Assert: no retry push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);

        // Assert: the notification remains failed at five attempts.
        var savedNotification =
            await _fixture.Context.NotificationLogs.FindAsync(
                notification.Id);

        Assert.NotNull(savedNotification);
        Assert.Equal(
            PushDeliveryStatus.Failed,
            savedNotification.PushDeliveryStatus);

        Assert.Equal(5, savedNotification.PushAttemptCount);
        Assert.Equal(
            "Maximum attempts already reached.",
            savedNotification.PushError);
    }
}