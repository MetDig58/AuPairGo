using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class ParentLocationAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ParentLocationAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Parent_cannot_submit_location_for_a_duty()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "parent-location-parent@test.com",
            FirstName = "Parent",
            LastName = "Location User",
            CellNumber = "0000000120",
            Role = "Parent",
            OneSignalPlayerId = "parent-location-device"
        };

        var auPairUser = new User
        {
            Email = "parent-location-aupair@test.com",
            FirstName = "Assigned",
            LastName = "Au Pair",
            CellNumber = "0000000121",
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
            Address = "Parent location test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Parent location test au pair",
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
            StreetAddress = "230 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0013",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "240 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0013",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an active duty assigned to the au pair.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Parent location authorization test",
            Description = "Parents may view locations but cannot submit them.",
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

        var locationRequest = new CreateLocationLogDto
        {
            DutyTaskId = duty.Id,
            Latitude = -26.2041,
            Longitude = 28.0473
        };

        // Act: the parent attempts to submit a location.
        var result = await controller.LogLocation(locationRequest);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: no location was saved.
        Assert.Empty(_fixture.Context.LocationLogs);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);

        // Assert: the duty remains InProgress.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);
    }
}