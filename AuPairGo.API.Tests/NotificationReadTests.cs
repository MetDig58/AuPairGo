using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationReadTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationReadTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Recipient_can_mark_their_own_notification_as_read()
    {
        // Arrange: create the sender and recipient users.
        var senderUser = new User
        {
            Email = "read-sender@test.com",
            FirstName = "Read",
            LastName = "Sender",
            CellNumber = "0000000180",
            Role = "AuPair",
            OneSignalPlayerId = "read-sender-device"
        };

        var recipientUser = new User
        {
            Email = "read-recipient-valid@test.com",
            FirstName = "Read",
            LastName = "Recipient",
            CellNumber = "0000000181",
            Role = "Parent",
            OneSignalPlayerId = "read-recipient-valid-device"
        };

        _fixture.Context.Users.AddRange(senderUser, recipientUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an unread notification for the recipient.
        var notification = new NotificationLog
        {
            SenderUserId = senderUser.Id,
            RecipientUserId = recipientUser.Id,
            Title = "Notification to read",
            Message = "The recipient should be able to mark this as read.",
            Type = NotificationType.General,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.Context.NotificationLogs.Add(notification);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as the notification recipient.
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new[]
                        {
                            new Claim(
                                ClaimTypes.NameIdentifier,
                                recipientUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: mark the notification as read.
        var result = await controller.MarkAsRead(notification.Id);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: the response contains the updated notification DTO.
        var responseNotification = Assert.IsType<
            NotificationResponseDto>(response.Value);

        Assert.Equal(notification.Id, responseNotification.Id);
        Assert.True(responseNotification.IsRead);

        // Assert: the database state changed from unread to read.
        var savedNotification =
            await _fixture.Context.NotificationLogs.FindAsync(
                notification.Id);

        Assert.NotNull(savedNotification);
        Assert.True(savedNotification.IsRead);

        // Assert: marking as read does not send push.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}