using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class RecentConversationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public RecentConversationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_can_retrieve_their_recent_conversations()
    {
        // Arrange: create the authenticated user and two conversation partners.
        var authenticatedUser = new User
        {
            Email = "recent-authenticated@test.com",
            FirstName = "Recent",
            LastName = "Authenticated",
            CellNumber = "0000000330",
            Role = "Parent",
            OneSignalPlayerId = "recent-authenticated-device"
        };

        var firstPartner = new User
        {
            Email = "recent-first-partner@test.com",
            FirstName = "First",
            LastName = "Partner",
            CellNumber = "0000000331",
            Role = "Parent",
            OneSignalPlayerId = "recent-first-device"
        };

        var secondPartner = new User
        {
            Email = "recent-second-partner@test.com",
            FirstName = "Second",
            LastName = "Partner",
            CellNumber = "0000000332",
            Role = "Parent",
            OneSignalPlayerId = "recent-second-device"
        };

        _fixture.Context.Users.AddRange(
            authenticatedUser,
            firstPartner,
            secondPartner);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create older and newer messages with the first partner.
        var olderFirstConversationMessage = new ChatMessage
        {
            SenderUserId = firstPartner.Id,
            RecipientUserId = authenticatedUser.Id,
            MessageText = "Older first conversation message",
            SentAt = DateTime.UtcNow.AddMinutes(-10),
            IsRead = true,
            ReadAt = DateTime.UtcNow.AddMinutes(-9)
        };

        var newerFirstConversationMessage = new ChatMessage
        {
            SenderUserId = authenticatedUser.Id,
            RecipientUserId = firstPartner.Id,
            MessageText = "Latest first conversation message",
            SentAt = DateTime.UtcNow.AddMinutes(-5),
            IsRead = false
        };

        // Arrange: create one message with the second partner.
        var secondConversationMessage = new ChatMessage
        {
            SenderUserId = secondPartner.Id,
            RecipientUserId = authenticatedUser.Id,
            MessageText = "Latest second conversation message",
            SentAt = DateTime.UtcNow.AddMinutes(-2),
            IsRead = false
        };

        _fixture.Context.ChatMessages.AddRange(
            olderFirstConversationMessage,
            newerFirstConversationMessage,
            secondConversationMessage);

        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as the user requesting their conversations.
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

        // Act: retrieve recent conversations.
        var result = await controller.GetRecentConversations(
            authenticatedUser.Id);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: one summary is returned for each conversation partner.
        var summaries = Assert.IsAssignableFrom<
            IEnumerable<object>>(response.Value);

        var summaryList = summaries.ToList();

        Assert.Equal(2, summaryList.Count);

        // Assert: the latest message from each partner is represented.
        Assert.Contains(
            summaryList,
            summary =>
                summary.GetType().GetProperty("otherUserId")?
                    .GetValue(summary) as int? == firstPartner.Id);

        Assert.Contains(
            summaryList,
            summary =>
                summary.GetType().GetProperty("otherUserId")?
                    .GetValue(summary) as int? == secondPartner.Id);

        // Assert: retrieving recent conversations sends no push.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}