using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class SosNotificationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public SosNotificationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_can_send_an_sos_alert_with_location()
    {
        // Arrange: create the sender and recipient users.
        var senderUser = new User
        {
            Email = "sos-sender@test.com",
            FirstName = "SOS",
            LastName = "Sender",
            CellNumber = "0000000210",
            Role = "AuPair",
            OneSignalPlayerId = "sos-sender-device"
        };

        var recipientUser = new User
        {
            Email = "sos-recipient@test.com",
            FirstName = "SOS",
            LastName = "Recipient",
            CellNumber = "0000000211",
            Role = "Parent",
            OneSignalPlayerId = "sos-recipient-device"
        };

        _fixture.Context.Users.AddRange(senderUser, recipientUser);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as the SOS sender.
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

        var request = new SendSosAlertDto
        {
            RecipientUserId = recipientUser.Id,
            Message = "Emergency assistance is required.",
            CurrentLatitude = -25.7479,
            CurrentLongitude = 28.2293
        };

        // Act: send the SOS alert.
        var result = await controller.SendSosAlert(request);

        // Assert: the API returns HTTP 201 Created.
        var response = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(
            StatusCodes.Status201Created,
            response.StatusCode);

        // Assert: the SOS notification was saved.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(senderUser.Id, notification.SenderUserId);
        Assert.Equal(recipientUser.Id, notification.RecipientUserId);
        Assert.Equal(
            NotificationType.SosAlert,
            notification.Type);

        Assert.Equal(
            "EMERGENCY SOS ALERT",
            notification.Title);

        Assert.Contains(
            "Emergency assistance is required.",
            notification.Message);

        Assert.Contains(
            "-25.7479",
            notification.Message);

        Assert.Contains(
            "28.2293",
            notification.Message);

        Assert.Equal(
            PushDeliveryStatus.Sent,
            notification.PushDeliveryStatus);

        // Assert: the recipient received one push.
        var pushAttempt = Assert.Single(fakeOneSignal.Attempts);

        Assert.Equal(
            "sos-recipient-device",
            pushAttempt.SubscriptionId);

        Assert.Equal(
            notification.Id,
            pushAttempt.NotificationId);
    }
}