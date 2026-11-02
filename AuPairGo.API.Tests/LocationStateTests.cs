using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationStateTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationStateTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Au_pair_cannot_submit_location_before_checking_in()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "location-parent@test.com",
            FirstName = "Location",
            LastName = "Parent",
            CellNumber = "0000000020",
            Role = "Parent",
            OneSignalPlayerId = "location-parent-device"
        };

        var auPairUser = new User
        {
            Email = "location-aupair@test.com",
            FirstName = "Location",
            LastName = "Au Pair",
            CellNumber = "0000000021",
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
            Address = "Location test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Location test au pair",
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
            StreetAddress = "30 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0003",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "40 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0003",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create a Pending duty.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Location state test duty",
            Description = "Location tracking must wait for check-in.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.Pending
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

        var locationRequest = new CreateLocationLogDto
        {
            DutyTaskId = duty.Id,
            Latitude = -25.7479,
            Longitude = 28.2293
        };

        // Act: submit location while the duty is still Pending.
        var result = await controller.LogLocation(locationRequest);

        // Assert: the API rejects the location with HTTP 409 Conflict.
        var response = Assert.IsType<ConflictObjectResult>(result);

        Assert.Equal(StatusCodes.Status409Conflict, response.StatusCode);

        // Assert: the duty remains Pending.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.Pending, savedDuty.Status);

        // Assert: no location was saved.
        var locationCount = _fixture.Context.LocationLogs.Count();

        Assert.Equal(0, locationCount);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}