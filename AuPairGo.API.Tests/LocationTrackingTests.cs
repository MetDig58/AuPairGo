using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationTrackingTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationTrackingTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Assigned_au_pair_can_submit_location_during_in_progress_duty()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "tracking-parent@test.com",
            FirstName = "Tracking",
            LastName = "Parent",
            CellNumber = "0000000030",
            Role = "Parent",
            OneSignalPlayerId = "tracking-parent-device"
        };

        var auPairUser = new User
        {
            Email = "tracking-aupair@test.com",
            FirstName = "Tracking",
            LastName = "Au Pair",
            CellNumber = "0000000031",
            Role = "AuPair"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the profiles linked to those users.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Tracking test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Tracking test au pair",
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
            StreetAddress = "50 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0004",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "60 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0004",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a duty that is already in progress.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Location tracking test duty",
            Description = "The au pair is already checked in.",
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

        // These Johannesburg coordinates are deliberately far from the
        // Pretoria pickup address, so no pickup-arrival notification is triggered.
        var locationRequest = new CreateLocationLogDto
        {
            DutyTaskId = duty.Id,
            Latitude = -26.2041,
            Longitude = 28.0473
        };

        // Act: submit a location while the duty is in progress.
        var result = await controller.LogLocation(locationRequest);

        // Assert: the API returns HTTP 201 Created.
        var response = Assert.IsType<CreatedAtActionResult>(result);

        Assert.Equal(StatusCodes.Status201Created, response.StatusCode);

        // Assert: exactly one location log was saved.
        var savedLocation = Assert.Single(
            _fixture.Context.LocationLogs);

        Assert.Equal(duty.Id, savedLocation.DutyTaskId);
        Assert.Equal(-26.2041, savedLocation.Latitude);
        Assert.Equal(28.0473, savedLocation.Longitude);

        // Assert: no push was attempted because the location was not near pickup.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}