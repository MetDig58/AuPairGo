using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class PickupGeofenceDuplicateTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public PickupGeofenceDuplicateTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Repeated_locations_near_pickup_create_only_one_arrival_notification()
    {
        // Arrange: create the users.
        var parentUser = new User
        {
            Email = "duplicate-parent@test.com",
            FirstName = "Duplicate",
            LastName = "Parent",
            CellNumber = "0000000050",
            Role = "Parent",
            OneSignalPlayerId = "duplicate-parent-device"
        };

        var auPairUser = new User
        {
            Email = "duplicate-aupair@test.com",
            FirstName = "Duplicate",
            LastName = "Au Pair",
            CellNumber = "0000000051",
            Role = "AuPair"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the profiles.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Duplicate geofence address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Duplicate geofence test au pair",
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
            StreetAddress = "90 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0006",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "100 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0006",
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
            Title = "Duplicate geofence test duty",
            Description = "Repeated nearby points must not duplicate alerts.",
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

        var locationRequest = new CreateLocationLogDto
        {
            DutyTaskId = duty.Id,
            Latitude = pickupAddress.latitude,
            Longitude = pickupAddress.longitude
        };

        // Act: submit the first nearby location.
        var firstResult = await controller.LogLocation(locationRequest);

        // Assert: the first location is accepted.
        Assert.IsType<CreatedAtActionResult>(firstResult);

        var firstNotification = Assert.Single(
            _fixture.Context.NotificationLogs);

        Assert.Equal(
            NotificationType.PickupArrival,
            firstNotification.Type);

        Assert.Single(fakeOneSignal.Attempts);

        // Act: submit another nearby location.
        var secondResult = await controller.LogLocation(locationRequest);

        // Assert: the second location is also accepted.
        Assert.IsType<CreatedAtActionResult>(secondResult);

        // Assert: both location points were saved.
        Assert.Equal(
            2,
            _fixture.Context.LocationLogs.Count());

        // Assert: only one arrival notification exists.
        var notifications = _fixture.Context.NotificationLogs.ToList();

        Assert.Single(notifications);
        Assert.Equal(
            NotificationType.PickupArrival,
            notifications[0].Type);

        // Assert: only one push was sent.
        Assert.Single(fakeOneSignal.Attempts);

        // Assert: the duty remains in progress and stays marked as notified.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);
        Assert.True(savedDuty.PickupArrivalNotified);
        Assert.NotNull(savedDuty.PickupArrivalNotifiedAt);
    }
}