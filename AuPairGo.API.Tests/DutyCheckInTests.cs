using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DutyCheckInTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DutyCheckInTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Assigned_au_pair_check_in_starts_duty_and_notifies_parent()
    {
        // Arrange: create the parent and au pair users.
        var parentUser = new User
        {
            Email = "parent@test.com",
            FirstName = "Test",
            LastName = "Parent",
            CellNumber = "0000000000",
            Role = "Parent",
            OneSignalPlayerId = "parent-device-123"
        };

        var auPairUser = new User
        {
            Email = "aupair@test.com",
            FirstName = "Test",
            LastName = "Au Pair",
            CellNumber = "0000000001",
            Role = "AuPair"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create profiles linked to the users.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Test au pair",
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
            StreetAddress = "1 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0001",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "2 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0001",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a pending duty linked to both profiles and addresses.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "School pickup",
            Description = "Pick up the child from school",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.Pending
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: replace the real OneSignal service with the fake one.
        var fakeOneSignal = new FakeOneSignalService();

        var controller = new DutiesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<DutiesController>.Instance);

        // Arrange: pretend the request was made by the assigned au pair.
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

        // Act: the assigned au pair checks in.
        var result = await controller.CheckInToDuty(duty.Id);

        // Assert: the controller returned HTTP 200.
        Assert.IsType<OkObjectResult>(result);

        // Assert: the duty was changed from Pending to InProgress.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);

        // Assert: exactly one manual check-in notification was saved.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(parentUser.Id, notification.RecipientUserId);
        Assert.Equal(auPairUser.Id, notification.SenderUserId);
        Assert.Equal(duty.Id, notification.DutyTaskId);
        Assert.Equal(NotificationType.ManualCheckIn, notification.Type);
        Assert.Equal(PushDeliveryStatus.Sent, notification.PushDeliveryStatus);

        // Assert: exactly one push attempt was recorded by the fake service.
        var pushAttempt = Assert.Single(fakeOneSignal.Attempts);

        Assert.Equal("parent-device-123", pushAttempt.SubscriptionId);
        Assert.Equal(notification.Id, pushAttempt.NotificationId);
        Assert.Equal(duty.Id, pushAttempt.DutyTaskId);
    }
}