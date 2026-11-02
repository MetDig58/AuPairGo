using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class SelfNotificationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public SelfNotificationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_send_a_notification_to_themselves()
    {
        // Arrange: create one user.
        var user = new User
        {
            Email = "self-notification@test.com",
            FirstName = "Self",
            LastName = "Notification",
            CellNumber = "0000000200",
            Role = "Parent",
            OneSignalPlayerId = "self-notification-device"
        };

        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as that same user.
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
                                user.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new SendNotificationDto
        {
            RecipientUserId = user.Id,
            Title = "Self notification",
            Message = "This should be rejected.",
            Type = NotificationType.General
        };

        // Act: attempt to send a notification to the same user.
        var result = await controller.SendNotification(request);

        // Assert: the API returns HTTP 400 Bad Request.
        var response = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            response.StatusCode);

        // Assert: no notification was saved.
        Assert.Empty(_fixture.Context.NotificationLogs);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}