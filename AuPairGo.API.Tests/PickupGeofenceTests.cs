using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class PickupGeofenceTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public PickupGeofenceTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Location_near_pickup_creates_one_arrival_notification_without_changing_duty_status()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "geofence-parent@test.com",
            FirstName = "Geofence",
            LastName = "Parent",
            CellNumber = "0000000040",
            Role = "Parent",
            OneSignalPlayerId = "geofence-parent-device"
        };

        var auPairUser = new User
        {
            Email = "geofence-aupair@test.com",
            FirstName = "Geofence",
            LastName = "Au Pair",
            CellNumber = "0000000041",
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
            Address = "Geofence test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Geofence test au pair",
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

        // Arrange: create pickup and drop-off addresses.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "70 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0005",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "80 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0005",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an InProgress duty.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Pickup geofence test duty",
            Description = "The au pair is arriving at the pickup point.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.InProgress
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new LocationController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<LocationController>.Instance);

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

        // Use the exact pickup coordinates, which are within the 100-metre geofence.
        var locationRequest = new CreateLocationLogDto
        {
            DutyTaskId = duty.Id,
            Latitude = pickupAddress.latitude,
            Longitude = pickupAddress.longitude
        };

        // Act: submit a location at the pickup point.
        var result = await controller.LogLocation(locationRequest);

        // Assert: the location request was accepted.
        var response = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);

        // Assert: the location was saved.
        var savedLocation = Assert.Single(
            _fixture.Context.LocationLogs);

        Assert.Equal(duty.Id, savedLocation.DutyTaskId);
        Assert.Equal(pickupAddress.latitude, savedLocation.Latitude);
        Assert.Equal(pickupAddress.longitude, savedLocation.Longitude);

        // Assert: exactly one pickup-arrival notification was created.
        var notification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(parentUser.Id, notification.RecipientUserId);
        Assert.Equal(auPairUser.Id, notification.SenderUserId);
        Assert.Equal(duty.Id, notification.DutyTaskId);
        Assert.Equal(NotificationType.PickupArrival, notification.Type);
        Assert.Equal(
            PushDeliveryStatus.Sent,
            notification.PushDeliveryStatus);

        // Assert: the fake OneSignal service recorded one push.
        var pushAttempt = Assert.Single(fakeOneSignal.Attempts);

        Assert.Equal(
            "geofence-parent-device",
            pushAttempt.SubscriptionId);

        Assert.Equal(notification.Id, pushAttempt.NotificationId);
        Assert.Equal(duty.Id, pushAttempt.DutyTaskId);

        // Assert: arrival does not change the duty to another state.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);
        Assert.True(savedDuty.PickupArrivalNotified);
        Assert.NotNull(savedDuty.PickupArrivalNotifiedAt);
    }
}