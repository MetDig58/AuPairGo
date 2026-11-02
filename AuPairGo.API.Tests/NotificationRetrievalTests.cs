using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class NotificationRetrievalTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public NotificationRetrievalTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_can_view_only_their_unread_notifications()
    {
        // Arrange: create the authenticated user and another user.
        var authenticatedUser = new User
        {
            Email = "retrieval-user@test.com",
            FirstName = "Retrieval",
            LastName = "User",
            CellNumber = "0000000160",
            Role = "Parent",
            OneSignalPlayerId = "retrieval-user-device"
        };

        var otherUser = new User
        {
            Email = "retrieval-other@test.com",
            FirstName = "Other",
            LastName = "User",
            CellNumber = "0000000161",
            Role = "AuPair",
            OneSignalPlayerId = "retrieval-other-device"
        };

        _fixture.Context.Users.AddRange(
            authenticatedUser,
            otherUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create one unread notification for the authenticated user.
        var unreadNotification = new NotificationLog
        {
            SenderUserId = otherUser.Id,
            RecipientUserId = authenticatedUser.Id,
            Title = "Unread notification",
            Message = "This notification should be returned.",
            Type = NotificationType.General,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Sent,
            CreatedAt = DateTime.UtcNow.AddMinutes(2)
        };

        // Arrange: create one read notification for the authenticated user.
        var readNotification = new NotificationLog
        {
            SenderUserId = otherUser.Id,
            RecipientUserId = authenticatedUser.Id,
            Title = "Read notification",
            Message = "This notification should be filtered out.",
            Type = NotificationType.ManualCheckIn,
            IsRead = true,
            PushDeliveryStatus = PushDeliveryStatus.Sent,
            CreatedAt = DateTime.UtcNow.AddMinutes(1)
        };

        // Arrange: create an unread notification for another user.
        var otherUserNotification = new NotificationLog
        {
            SenderUserId = authenticatedUser.Id,
            RecipientUserId = otherUser.Id,
            Title = "Other user's notification",
            Message = "This must never be returned.",
            Type = NotificationType.SosAlert,
            IsRead = false,
            PushDeliveryStatus = PushDeliveryStatus.Sent,
            CreatedAt = DateTime.UtcNow.AddMinutes(3)
        };

        _fixture.Context.NotificationLogs.AddRange(
            unreadNotification,
            readNotification,
            otherUserNotification);

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
                                authenticatedUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: request only unread notifications for the authenticated user.
        var result = await controller.GetUserNotifications(
            authenticatedUser.Id,
            unreadOnly: true);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: the response contains exactly one notification.
        var notifications = Assert.IsType<
            List<NotificationResponseDto>>(response.Value);

        var notification = Assert.Single(notifications);

        Assert.Equal(
            unreadNotification.Id,
            notification.Id);

        Assert.Equal(
            authenticatedUser.Id,
            notification.RecipientUserId);

        Assert.Equal(
            "Unread notification",
            notification.Title);

        Assert.Equal(
            "This notification should be returned.",
            notification.Message);

        Assert.Equal(
            "General",
            notification.Type);

        Assert.False(notification.IsRead);

        // Assert: another user's notification was not returned.
        Assert.DoesNotContain(
            notifications,
            item => item.Id == otherUserNotification.Id);

        // Assert: reading notifications does not send push notifications.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}