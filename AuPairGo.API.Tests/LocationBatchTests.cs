using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationBatchTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationBatchTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Assigned_au_pair_can_upload_multiple_offline_locations()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "batch-parent@test.com",
            FirstName = "Batch",
            LastName = "Parent",
            CellNumber = "0000000060",
            Role = "Parent",
            OneSignalPlayerId = "batch-parent-device"
        };

        var auPairUser = new User
        {
            Email = "batch-aupair@test.com",
            FirstName = "Batch",
            LastName = "Au Pair",
            CellNumber = "0000000061",
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
            Address = "Batch test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Batch test au pair",
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
            StreetAddress = "110 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0007",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "120 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0007",
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
            Title = "Offline batch test duty",
            Description = "Queued locations should be uploaded together.",
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

        var firstCapturedAt = new DateTimeOffset(
            2026,
            11,
            2,
            7,
            0,
            0,
            TimeSpan.Zero);

        var secondCapturedAt = firstCapturedAt.AddMinutes(5);

        var batchRequest = new CreateLocationLogBatchDto
        {
            DutyTaskId = duty.Id,
            Locations = new List<OfflineLocationPointDto>
            {
                new()
                {
                    Latitude = -26.2041,
                    Longitude = 28.0473,
                    CapturedAt = secondCapturedAt
                },
                new()
                {
                    Latitude = -26.2050,
                    Longitude = 28.0480,
                    CapturedAt = firstCapturedAt
                }
            }
        };

        // Act: upload both offline points together.
        var result = await controller.LogLocationBatch(batchRequest);

        // Assert: the batch endpoint returns HTTP 200.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);

        // Assert: both points were saved.
        var savedLocations = _fixture.Context.LocationLogs
            .OrderBy(location => location.TimeStamp)
            .ToList();

        Assert.Equal(2, savedLocations.Count);

        // Assert: the controller preserved the original capture times
        // and ordered the saved records chronologically.
        Assert.Equal(
            duty.Id,
            savedLocations[0].DutyTaskId);

        Assert.Equal(
            firstCapturedAt.UtcDateTime,
            savedLocations[0].TimeStamp);

        Assert.Equal(
            -26.2050,
            savedLocations[0].Latitude);

        Assert.Equal(
            28.0480,
            savedLocations[0].Longitude);

        Assert.Equal(
            secondCapturedAt.UtcDateTime,
            savedLocations[1].TimeStamp);

        Assert.Equal(
            -26.2041,
            savedLocations[1].Latitude);

        Assert.Equal(
            28.0473,
            savedLocations[1].Longitude);

        // Assert: the points were far from pickup, so no arrival notification
        // or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}