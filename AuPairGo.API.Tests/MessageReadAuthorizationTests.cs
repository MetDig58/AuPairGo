using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class MessageReadAuthorizationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public MessageReadAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sender_cannot_mark_their_own_sent_message_as_read()
    {
        // Arrange: create sender and recipient users.
        var senderUser = new User
        {
            Email = "message-read-auth-sender@test.com",
            FirstName = "Read Auth",
            LastName = "Sender",
            CellNumber = "0000000320",
            Role = "Parent",
            OneSignalPlayerId = "read-auth-sender-device"
        };

        var recipientUser = new User
        {
            Email = "message-read-auth-recipient@test.com",
            FirstName = "Read Auth",
            LastName = "Recipient",
            CellNumber = "0000000321",
            Role = "Parent",
            OneSignalPlayerId = "read-auth-recipient-device"
        };

        _fixture.Context.Users.AddRange(senderUser, recipientUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an unread message sent to the recipient.
        var message = new ChatMessage
        {
            SenderUserId = senderUser.Id,
            RecipientUserId = recipientUser.Id,
            MessageText = "Only the recipient can mark this as read.",
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

        // Arrange: authenticate as the sender, not the recipient.
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

        // Act: the sender attempts to mark the message as read.
        var result = await controller.MarkMessagesAsRead(message.Id);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: the message remains unread.
        var savedMessage =
            await _fixture.Context.ChatMessages.FindAsync(message.Id);

        Assert.NotNull(savedMessage);
        Assert.False(savedMessage.IsRead);
        Assert.Null(savedMessage.ReadAt);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}