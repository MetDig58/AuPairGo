using AuPairGo.API.Data;
using AuPairGo.API.Models;
using AuPairGo.API.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationRetryDeviceTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationRetryDeviceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Retry_worker_waits_when_recipient_has_no_device()
    {
        // Arrange: create a recipient without a registered OneSignal device.
        var recipient = new User
        {
            Email = "retry-no-device-recipient@test.com",
            FirstName = "Retry No Device",
            LastName = "Recipient",
            CellNumber = "0000000390",
            Role = "Parent",
            OneSignalPlayerId = string.Empty
        };

        var sender = new User
        {
            Email = "retry-no-device-sender@test.com",
            FirstName = "Retry No Device",
            LastName = "Sender",
            CellNumber = "0000000391",
            Role = "AuPair",
            OneSignalPlayerId = "retry-no-device-sender-device"
        };

        _fixture.Context.Users.AddRange(recipient, sender);
        await _fixture.Context.SaveChangesAsync();

        var notification = new NotificationLog
        {
            SenderUserId = sender.Id,
            RecipientUserId = recipient.Id,
            Title = "Waiting notification",
            Message = "The recipient still needs to register a device.",
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

        var services = new ServiceCollection();

        services.AddSingleton<ApplicationDbContext>(_fixture.Context);
        services.AddSingleton<IOneSignalService>(fakeOneSignal);

        using var serviceProvider = services.BuildServiceProvider();

        var worker = new NotificationRetryWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotificationRetryWorker>.Instance);

        using var cancellationSource = new CancellationTokenSource();

        // Act: run the retry worker.
        await worker.StartAsync(cancellationSource.Token);

        await Task.Delay(250);

        cancellationSource.Cancel();

        await worker.StopAsync(CancellationToken.None);

        // Assert: OneSignal was not called without a device ID.
        Assert.Empty(fakeOneSignal.Attempts);

        var savedNotification =
            await _fixture.Context.NotificationLogs.FindAsync(
                notification.Id);

        Assert.NotNull(savedNotification);

        // The worker keeps it failed so it can be reconsidered later.
        Assert.Equal(
            PushDeliveryStatus.Failed,
            savedNotification.PushDeliveryStatus);

        // No push attempt occurred, so the count does not increase.
        Assert.Equal(1, savedNotification.PushAttemptCount);

        Assert.Equal(
            "Recipient has no registered OneSignal subscription.",
            savedNotification.PushError);

        Assert.NotNull(savedNotification.PushLastAttemptAt);
    }
}