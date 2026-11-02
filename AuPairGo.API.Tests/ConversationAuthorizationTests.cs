using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class ConversationAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ConversationAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_view_a_conversation_between_two_other_users()
    {
        // Arrange: create three users.
        var firstUser = new User
        {
            Email = "conversation-first@test.com",
            FirstName = "Conversation",
            LastName = "First",
            CellNumber = "0000000290",
            Role = "Parent",
            OneSignalPlayerId = "conversation-first-device"
        };

        var secondUser = new User
        {
            Email = "conversation-second@test.com",
            FirstName = "Conversation",
            LastName = "Second",
            CellNumber = "0000000291",
            Role = "Parent",
            OneSignalPlayerId = "conversation-second-device"
        };

        var unauthorizedUser = new User
        {
            Email = "conversation-unauthorized@test.com",
            FirstName = "Conversation",
            LastName = "Unauthorized",
            CellNumber = "0000000292",
            Role = "Parent",
            OneSignalPlayerId = "conversation-unauthorized-device"
        };

        _fixture.Context.Users.AddRange(
            firstUser,
            secondUser,
            unauthorizedUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a message between the first and second users.
        var message = new ChatMessage
        {
            SenderUserId = firstUser.Id,
            RecipientUserId = secondUser.Id,
            MessageText = "Private conversation message.",
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

        // Arrange: authenticate as the unrelated third user.
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

        // Act: request the conversation between the other two users.
        var result = await controller.GetConversations(
            firstUser.Id,
            secondUser.Id);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: no push is sent by a read operation.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}