using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_cannot_view_another_users_notifications()
    {
        // Arrange: create two users.
        var firstUser = new User
        {
            Email = "notification-first@test.com",
            FirstName = "First",
            LastName = "User",
            CellNumber = "0000000150",
            Role = "Parent",
            OneSignalPlayerId = "first-user-device"
        };

        var secondUser = new User
        {
            Email = "notification-second@test.com",
            FirstName = "Second",
            LastName = "User",
            CellNumber = "0000000151",
            Role = "AuPair",
            OneSignalPlayerId = "second-user-device"
        };

        _fixture.Context.Users.AddRange(firstUser, secondUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a notification belonging to the second user.
        var secondUserNotification = new NotificationLog
        {
            SenderUserId = firstUser.Id,
            RecipientUserId = secondUser.Id,
            Title = "Private notification",
            Message = "This belongs only to the second user.",
            Type = NotificationType.General,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _fixture.Context.NotificationLogs.Add(secondUserNotification);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as the first user.
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

        // Act: the first user tries to view the second user's notifications.
        var result = await controller.GetUserNotifications(secondUser.Id);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: no push is sent by a read operation.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}