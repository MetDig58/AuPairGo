using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class MessagePushFailureTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public MessagePushFailureTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Message_is_saved_when_message_push_delivery_fails()
    {
        // Arrange: create parent and au pair users.
        var parentUser = new User
        {
            Email = "message-failure-parent@test.com",
            FirstName = "Message Failure",
            LastName = "Parent",
            CellNumber = "0000000350",
            Role = "Parent",
            OneSignalPlayerId = "message-failure-parent-device"
        };

        var auPairUser = new User
        {
            Email = "message-failure-aupair@test.com",
            FirstName = "Message Failure",
            LastName = "Au Pair",
            CellNumber = "0000000351",
            Role = "AuPair",
            OneSignalPlayerId = "message-failure-aupair-device"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create profiles.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Message push failure address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Message push failure au pair",
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
            FirstName = "Failure",
            LastName = "Child",
            Allergies = "None",
            SchoolName = "Test School",
            SpecialInstructions = "None"
        };

        _fixture.Context.ChildProfiles.Add(childProfile);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the required addresses.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "310 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0017",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "320 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0017",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an eligible, non-cancelled duty.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            ChildProfileId = childProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Message push failure duty",
            Description = "This duty permits parent-au-pair messaging.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.InProgress
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var failingOneSignal = new FailingOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            failingOneSignal,
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
            MessageText = "This message must survive a push failure."
        };

        // Act: send the message while push delivery fails.
        var result = await controller.SendMessage(request);

        // Assert: the message endpoint still returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: the chat message was saved.
        var savedMessage = Assert.Single(
            _fixture.Context.ChatMessages);

        Assert.Equal(parentUser.Id, savedMessage.SenderUserId);
        Assert.Equal(auPairUser.Id, savedMessage.RecipientUserId);
        Assert.Equal(
            "This message must survive a push failure.",
            savedMessage.MessageText);

        // Assert: the notification was saved but marked failed.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(NotificationType.General, notification.Type);
        Assert.Equal(
            PushDeliveryStatus.Failed,
            notification.PushDeliveryStatus);

        Assert.Equal(
            "Simulated OneSignal failure.",
            notification.PushError);

        Assert.Equal(1, notification.PushAttemptCount);

        // Assert: OneSignal was attempted once.
        Assert.Equal(1, failingOneSignal.AttemptCount);
    }
}