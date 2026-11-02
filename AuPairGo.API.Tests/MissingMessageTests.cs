using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class MissingMessageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public MissingMessageTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Marking_a_missing_message_returns_not_found()
    {
        // Arrange: create an authenticated user.
        var user = new User
        {
            Email = "missing-message-user@test.com",
            FirstName = "Missing",
            LastName = "Message User",
            CellNumber = "0000000400",
            Role = "Parent",
            OneSignalPlayerId = "missing-message-device"
        };

        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

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

        const int missingMessageId = 999999;

        // Act: attempt to mark a message that does not exist.
        var result = await controller.MarkMessagesAsRead(
            missingMessageId);

        // Assert: the API should return HTTP 404 Not Found.
        var response = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            response.StatusCode);

        // Assert: no message or push was created.
        Assert.Empty(_fixture.Context.ChatMessages);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}