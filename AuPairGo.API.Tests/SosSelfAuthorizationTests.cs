using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class SosSelfAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public SosSelfAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_send_an_sos_alert_to_themselves()
    {
        // Arrange: create one user.
        var user = new User
        {
            Email = "sos-self@test.com",
            FirstName = "SOS",
            LastName = "Self User",
            CellNumber = "0000000220",
            Role = "Parent",
            OneSignalPlayerId = "sos-self-device"
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

        var request = new SendSosAlertDto
        {
            RecipientUserId = user.Id,
            Message = "This SOS request should be rejected.",
            CurrentLatitude = -25.7479,
            CurrentLongitude = 28.2293
        };

        // Act: attempt to send an SOS alert to the same user.
        var result = await controller.SendSosAlert(request);

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