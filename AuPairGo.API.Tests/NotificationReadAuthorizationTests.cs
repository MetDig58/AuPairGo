using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationReadAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationReadAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_mark_another_users_notification_as_read()
    {
        // Arrange: create the notification recipient and another user.
        var recipientUser = new User
        {
            Email = "read-recipient@test.com",
            FirstName = "Read",
            LastName = "Recipient",
            CellNumber = "0000000170",
            Role = "Parent",
            OneSignalPlayerId = "read-recipient-device"
        };

        var unauthorizedUser = new User
        {
            Email = "read-unauthorized@test.com",
            FirstName = "Read",
            LastName = "Unauthorized",
            CellNumber = "0000000171",
            Role = "AuPair",
            OneSignalPlayerId = "read-unauthorized-device"
        };

        _fixture.Context.Users.AddRange(
            recipientUser,
            unauthorizedUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an unread notification for the recipient.
        var notification = new NotificationLog
        {
            SenderUserId = unauthorizedUser.Id,
            RecipientUserId = recipientUser.Id,
            Title = "Unread private notification",
            Message = "Only the recipient may mark this as read.",
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

        // Arrange: authenticate as the user who is not the recipient.
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
                                unauthorizedUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: the unauthorized user attempts to mark the notification as read.
        var result = await controller.MarkAsRead(notification.Id);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: the notification remains unread.
        var savedNotification =
            await _fixture.Context.NotificationLogs.FindAsync(
                notification.Id);

        Assert.NotNull(savedNotification);
        Assert.False(savedNotification.IsRead);

        // Assert: marking a notification as read does not send push.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}