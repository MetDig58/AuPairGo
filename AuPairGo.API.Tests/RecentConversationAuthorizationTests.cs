using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class RecentConversationAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public RecentConversationAuthorizationTests(
        TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_view_another_users_recent_conversations()
    {
        // Arrange: create the requested user and the authenticated user.
        var requestedUser = new User
        {
            Email = "recent-requested@test.com",
            FirstName = "Requested",
            LastName = "User",
            CellNumber = "0000000340",
            Role = "Parent",
            OneSignalPlayerId = "recent-requested-device"
        };

        var authenticatedUser = new User
        {
            Email = "recent-unauthorized@test.com",
            FirstName = "Unauthorized",
            LastName = "User",
            CellNumber = "0000000341",
            Role = "Parent",
            OneSignalPlayerId = "recent-unauthorized-device"
        };

        _fixture.Context.Users.AddRange(
            requestedUser,
            authenticatedUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a message belonging to the requested user.
        var message = new ChatMessage
        {
            SenderUserId = requestedUser.Id,
            RecipientUserId = authenticatedUser.Id,
            MessageText = "Private recent conversation.",
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _fixture.Context.ChatMessages.Add(message);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as a different user.
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
                                authenticatedUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: request the other user's recent conversations.
        var result = await controller.GetRecentConversations(
            requestedUser.Id);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: no push is sent by the read operation.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}