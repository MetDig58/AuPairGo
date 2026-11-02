using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationAuthorizationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Unassigned_au_pair_cannot_submit_location_for_someone_elses_duty()
    {
        // Arrange: create the parent, assigned au pair, and unauthorized au pair.
        var parentUser = new User
        {
            Email = "location-auth-parent@test.com",
            FirstName = "Location Auth",
            LastName = "Parent",
            CellNumber = "0000000110",
            Role = "Parent",
            OneSignalPlayerId = "location-auth-parent-device"
        };

        var assignedAuPairUser = new User
        {
            Email = "location-auth-assigned@test.com",
            FirstName = "Assigned",
            LastName = "Au Pair",
            CellNumber = "0000000111",
            Role = "AuPair"
        };

        var unauthorizedAuPairUser = new User
        {
            Email = "location-auth-unauthorized@test.com",
            FirstName = "Unauthorized",
            LastName = "Au Pair",
            CellNumber = "0000000112",
            Role = "AuPair"
        };

        _fixture.Context.Users.AddRange(
            parentUser,
            assignedAuPairUser,
            unauthorizedAuPairUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create profiles for all users.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Location authorization address"
        };

        var assignedAuPairProfile = new AuPairProfile
        {
            UserId = assignedAuPairUser.Id,
            Bio = "Assigned location test au pair",
            Suburb = "Pretoria",
            HasDriversLicense = true,
            HasCar = true,
            HasFirstAid = true,
            YearsOfExperience = 2,
            Availability = "Weekdays"
        };

        var unauthorizedAuPairProfile = new AuPairProfile
        {
            UserId = unauthorizedAuPairUser.Id,
            Bio = "Unauthorized location test au pair",
            Suburb = "Pretoria",
            HasDriversLicense = true,
            HasCar = true,
            HasFirstAid = true,
            YearsOfExperience = 1,
            Availability = "Weekends"
        };

        _fixture.Context.ParentProfiles.Add(parentProfile);
        _fixture.Context.AuPairProfiles.AddRange(
            assignedAuPairProfile,
            unauthorizedAuPairProfile);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the addresses required by DutyTask.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "210 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0012",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "220 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0012",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: assign the in-progress duty to the assigned au pair only.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = assignedAuPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Location authorization test",
            Description = "Only the assigned au pair may submit locations.",
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

        // Arrange: authenticate as the au pair who is not assigned.
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
                                unauthorizedAuPairUser.Id.ToString())
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

        // Act: the unauthorized au pair submits a location.
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