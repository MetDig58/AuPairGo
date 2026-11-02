using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class SelfMessageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public SelfMessageTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_send_a_message_to_themselves()
    {
        // Arrange: create one user.
        var user = new User
        {
            Email = "self-message@test.com",
            FirstName = "Self",
            LastName = "Message",
            CellNumber = "0000000280",
            Role = "Parent",
            OneSignalPlayerId = "self-message-device"
        };

        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as that user.
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

        var request = new SendMessageDto
        {
            RecipientUserId = user.Id,
            MessageText = "This message should be rejected."
        };

        // Act: attempt to message the same user.
        var result = await controller.SendMessage(request);

        // Assert: the API returns HTTP 400 Bad Request.
        var response = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            response.StatusCode);

        // Assert: no message or notification was saved.
        Assert.Empty(_fixture.Context.ChatMessages);
        Assert.Empty(_fixture.Context.NotificationLogs);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}