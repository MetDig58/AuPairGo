using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class ConversationRetrievalTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ConversationRetrievalTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Conversation_participant_can_retrieve_their_conversation()
    {
        // Arrange: create two parent users and an unrelated third user.
        var firstUser = new User
        {
            Email = "conversation-retrieval-first@test.com",
            FirstName = "Conversation",
            LastName = "First",
            CellNumber = "0000000300",
            Role = "Parent",
            OneSignalPlayerId = "conversation-retrieval-first-device"
        };

        var secondUser = new User
        {
            Email = "conversation-retrieval-second@test.com",
            FirstName = "Conversation",
            LastName = "Second",
            CellNumber = "0000000301",
            Role = "Parent",
            OneSignalPlayerId = "conversation-retrieval-second-device"
        };

        var unrelatedUser = new User
        {
            Email = "conversation-retrieval-unrelated@test.com",
            FirstName = "Conversation",
            LastName = "Unrelated",
            CellNumber = "0000000302",
            Role = "Parent",
            OneSignalPlayerId = "conversation-retrieval-unrelated-device"
        };

        _fixture.Context.Users.AddRange(
            firstUser,
            secondUser,
            unrelatedUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create two messages in reverse insertion order.
        var laterMessage = new ChatMessage
        {
            SenderUserId = secondUser.Id,
            RecipientUserId = firstUser.Id,
            MessageText = "Second message",
            SentAt = DateTime.UtcNow.AddMinutes(2),
            IsRead = false
        };

        var earlierMessage = new ChatMessage
        {
            SenderUserId = firstUser.Id,
            RecipientUserId = secondUser.Id,
            MessageText = "First message",
            SentAt = DateTime.UtcNow.AddMinutes(1),
            IsRead = true,
            ReadAt = DateTime.UtcNow.AddMinutes(3)
        };

        // This message belongs to another conversation and must not be returned.
        var unrelatedMessage = new ChatMessage
        {
            SenderUserId = firstUser.Id,
            RecipientUserId = unrelatedUser.Id,
            MessageText = "Unrelated conversation message",
            SentAt = DateTime.UtcNow.AddMinutes(3),
            IsRead = false
        };

        _fixture.Context.ChatMessages.AddRange(
            laterMessage,
            earlierMessage,
            unrelatedMessage);

        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as the first conversation participant.
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
                                firstUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: retrieve the conversation.
        var result = await controller.GetConversations(
            firstUser.Id,
            secondUser.Id);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: exactly the two conversation messages are returned.
        var messages = Assert.IsType<
            List<ChatMessageResponseDto>>(response.Value);

        Assert.Equal(2, messages.Count);

        // Assert: messages are ordered from oldest to newest.
        Assert.Equal(
            earlierMessage.Id,
            messages[0].Id);

        Assert.Equal(
            "First message",
            messages[0].MessageText);

        Assert.Equal(
            laterMessage.Id,
            messages[1].Id);

        Assert.Equal(
            "Second message",
            messages[1].MessageText);

        // Assert: read state is mapped correctly.
        Assert.True(messages[0].IsRead);
        Assert.NotNull(messages[0].ReadAt);

        Assert.False(messages[1].IsRead);
        Assert.Null(messages[1].ReadAt);

        // Assert: the unrelated conversation was excluded.
        Assert.DoesNotContain(
            messages,
            message => message.Id == unrelatedMessage.Id);

        // Assert: retrieving a conversation does not send push notifications.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}