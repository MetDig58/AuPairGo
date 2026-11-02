using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class MessageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public MessageTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Parent_can_send_message_to_assigned_au_pair()
    {
        // Arrange: create the parent and au pair users.
        var parentUser = new User
        {
            Email = "message-parent@test.com",
            FirstName = "Message",
            LastName = "Parent",
            CellNumber = "0000000260",
            Role = "Parent",
            OneSignalPlayerId = "message-parent-device"
        };

        var auPairUser = new User
        {
            Email = "message-aupair@test.com",
            FirstName = "Message",
            LastName = "Au Pair",
            CellNumber = "0000000261",
            Role = "AuPair",
            OneSignalPlayerId = "message-aupair-device"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the parent and au pair profiles.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Message test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Message test au pair",
            Suburb = "Pretoria",
            HasDriversLicense = true,
            HasCar = true,
            HasFirstAid = true,
            YearsOfExperience = 2,
            Availability = "Weekdays"
        };

        _fixture.Context.ParentProfiles.Add(parentProfile);
        _fixture.Context.AuPairProfiles.Add(auPairProfile);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a child required for parent-au-pair messaging.
        var childProfile = new ChildProfile
        {
            UserId = parentUser.Id,
            FirstName = "Test",
            LastName = "Child",
            Allergies = "None",
            SchoolName = "Test School",
            SpecialInstructions = "None"
        };

        _fixture.Context.ChildProfiles.Add(childProfile);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the addresses required by DutyTask.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "290 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0016",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "300 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0016",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an active duty linking the parent, au pair, and child.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            ChildProfileId = childProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Message test duty",
            Description = "This duty permits parent-au-pair messaging.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.InProgress
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as the parent.
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
                                parentUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new SendMessageDto
        {
            RecipientUserId = auPairUser.Id,
            MessageText = "Please confirm the pickup time."
        };

        // Act: send the message.
        var result = await controller.SendMessage(request);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: the chat message was saved.
        var message = Assert.Single(
            _fixture.Context.ChatMessages);

        Assert.Equal(parentUser.Id, message.SenderUserId);
        Assert.Equal(auPairUser.Id, message.RecipientUserId);
        Assert.Equal(
            "Please confirm the pickup time.",
            message.MessageText);
        Assert.False(message.IsRead);

        // Assert: a notification was created for the recipient.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(parentUser.Id, notification.SenderUserId);
        Assert.Equal(auPairUser.Id, notification.RecipientUserId);
        Assert.Equal(NotificationType.General, notification.Type);
        Assert.Equal("New message", notification.Title);
        Assert.Equal(
            "Please confirm the pickup time.",
            notification.Message);
        Assert.Equal(
            PushDeliveryStatus.Sent,
            notification.PushDeliveryStatus);

        // Assert: the recipient received one push.
        var pushAttempt = Assert.Single(fakeOneSignal.Attempts);

        Assert.Equal(
            "message-aupair-device",
            pushAttempt.SubscriptionId);

        Assert.Equal(
            notification.Id,
            pushAttempt.NotificationId);

        // Messages are not currently linked to a duty in the notification record.
        Assert.Equal(0, pushAttempt.DutyTaskId);
    }
}