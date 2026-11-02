using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class GeneralNotificationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public GeneralNotificationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Authenticated_user_can_send_a_general_notification()
    {
        // Arrange: create the sender and recipient users.
        var senderUser = new User
        {
            Email = "general-sender@test.com",
            FirstName = "General",
            LastName = "Sender",
            CellNumber = "0000000190",
            Role = "Parent",
            OneSignalPlayerId = "general-sender-device"
        };

        var recipientUser = new User
        {
            Email = "general-recipient@test.com",
            FirstName = "General",
            LastName = "Recipient",
            CellNumber = "0000000191",
            Role = "AuPair",
            OneSignalPlayerId = "general-recipient-device"
        };

        _fixture.Context.Users.AddRange(senderUser, recipientUser);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as the sender.
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
                                senderUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new SendNotificationDto
        {
            RecipientUserId = recipientUser.Id,
            Title = "Schedule update",
            Message = "Your duty schedule has been updated.",
            Type = NotificationType.General
        };

        // Act: send the notification.
        var result = await controller.SendNotification(request);

        // Assert: the API returns HTTP 201 Created.
        var response = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(
            StatusCodes.Status201Created,
            response.StatusCode);

        // Assert: the notification was persisted.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(senderUser.Id, notification.SenderUserId);
        Assert.Equal(recipientUser.Id, notification.RecipientUserId);
        Assert.Equal("Schedule update", notification.Title);
        Assert.Equal(
            "Your duty schedule has been updated.",
            notification.Message);
        Assert.Equal(NotificationType.General, notification.Type);
        Assert.False(notification.IsRead);
        Assert.Equal(
            PushDeliveryStatus.Sent,
            notification.PushDeliveryStatus);

        // Assert: the recipient received one push.
        var pushAttempt = Assert.Single(fakeOneSignal.Attempts);

        Assert.Equal(
            "general-recipient-device",
            pushAttempt.SubscriptionId);

        Assert.Equal(notification.Id, pushAttempt.NotificationId);

        // General notifications are not tied to a duty.
        Assert.Equal(0, pushAttempt.DutyTaskId);
    }
}