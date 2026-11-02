using System.Net;
using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using AuPairGo.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class PushFailureTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public PushFailureTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Check_in_is_saved_when_push_delivery_fails()
    {
        // Arrange: create users.
        var parentUser = new User
        {
            Email = "failure-parent@test.com",
            FirstName = "Failure",
            LastName = "Parent",
            CellNumber = "0000000070",
            Role = "Parent",
            OneSignalPlayerId = "failure-parent-device"
        };

        var auPairUser = new User
        {
            Email = "failure-aupair@test.com",
            FirstName = "Failure",
            LastName = "Au Pair",
            CellNumber = "0000000071",
            Role = "AuPair"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create profiles.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Push failure test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Push failure test au pair",
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

        // Arrange: create the addresses required by DutyTask.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "130 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0008",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "140 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0008",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a pending duty.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Push failure test duty",
            Description = "The check-in must survive a push failure.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.Pending
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var failingOneSignal = new FailingOneSignalService();

        var controller = new DutiesController(
            _fixture.Context,
            failingOneSignal,
            NullLogger<DutiesController>.Instance);

        // Arrange: authenticate as the assigned au pair.
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
                                auPairUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        // Act: check in while OneSignal is configured to fail.
        var result = await controller.CheckInToDuty(duty.Id);

        // Assert: the API reports an upstream push failure.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status502BadGateway,
            response.StatusCode);

        // Assert: the duty check-in itself was still saved.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);

        // Assert: the notification was preserved and marked as failed.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(
            NotificationType.ManualCheckIn,
            notification.Type);

        Assert.Equal(
            PushDeliveryStatus.Failed,
            notification.PushDeliveryStatus);

        Assert.Equal(
            "Simulated OneSignal failure.",
            notification.PushError);

        // Assert: the fake attempted one push before failing.
        Assert.Equal(1, failingOneSignal.AttemptCount);
    }
}

public sealed class FailingOneSignalService : IOneSignalService
{
    public int AttemptCount { get; private set; }

    public Task SendPushNotificationAsync(
        string subscriptionId,
        string title,
        string message,
        int notificationId,
        int dutyTaskId)
    {
        AttemptCount++;

        throw new HttpRequestException(
            "Simulated OneSignal failure.",
            null,
            HttpStatusCode.BadGateway);
    }
}