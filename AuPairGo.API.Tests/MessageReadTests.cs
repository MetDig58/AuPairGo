using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class MessageReadTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public MessageReadTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Recipient_can_mark_their_message_as_read()
    {
        // Arrange: create sender and recipient users.
        var senderUser = new User
        {
            Email = "message-read-sender@test.com",
            FirstName = "Message Read",
            LastName = "Sender",
            CellNumber = "0000000310",
            Role = "Parent",
            OneSignalPlayerId = "message-read-sender-device"
        };

        var recipientUser = new User
        {
            Email = "message-read-recipient@test.com",
            FirstName = "Message Read",
            LastName = "Recipient",
            CellNumber = "0000000311",
            Role = "Parent",
            OneSignalPlayerId = "message-read-recipient-device"
        };

        _fixture.Context.Users.AddRange(senderUser, recipientUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an unread message for the recipient.
        var message = new ChatMessage
        {
            SenderUserId = senderUser.Id,
            RecipientUserId = recipientUser.Id,
            MessageText = "Please mark this message as read.",
            SentAt = DateTime.UtcNow,
            IsRead = false,
            ReadAt = null
        };

        _fixture.Context.ChatMessages.Add(message);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as the message recipient.
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
                                recipientUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: mark the message as read.
        var result = await controller.MarkMessagesAsRead(message.Id);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: the response contains the updated message.
        var responseMessage = Assert.IsType<
            ChatMessageResponseDto>(response.Value);

        Assert.Equal(message.Id, responseMessage.Id);
        Assert.True(responseMessage.IsRead);
        Assert.NotNull(responseMessage.ReadAt);

        // Assert: the database state was updated.
        var savedMessage =
            await _fixture.Context.ChatMessages.FindAsync(message.Id);

        Assert.NotNull(savedMessage);
        Assert.True(savedMessage.IsRead);
        Assert.NotNull(savedMessage.ReadAt);

        // Assert: marking a message as read does not send push.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}