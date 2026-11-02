using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationBatchAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationBatchAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Unassigned_au_pair_cannot_upload_location_batch_for_someone_elses_duty()
    {
        // Arrange: create the parent, assigned au pair, and unauthorized au pair.
        var parentUser = new User
        {
            Email = "batch-auth-parent@test.com",
            FirstName = "Batch Auth",
            LastName = "Parent",
            CellNumber = "0000000130",
            Role = "Parent",
            OneSignalPlayerId = "batch-auth-parent-device"
        };

        var assignedAuPairUser = new User
        {
            Email = "batch-auth-assigned@test.com",
            FirstName = "Assigned",
            LastName = "Batch Au Pair",
            CellNumber = "0000000131",
            Role = "AuPair"
        };

        var unauthorizedAuPairUser = new User
        {
            Email = "batch-auth-unauthorized@test.com",
            FirstName = "Unauthorized",
            LastName = "Batch Au Pair",
            CellNumber = "0000000132",
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
            Address = "Batch authorization address"
        };

        var assignedAuPairProfile = new AuPairProfile
        {
            UserId = assignedAuPairUser.Id,
            Bio = "Assigned batch test au pair",
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
            Bio = "Unauthorized batch test au pair",
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
            StreetAddress = "250 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0014",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "260 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0014",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: assign the active duty only to the assigned au pair.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = assignedAuPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Batch authorization test",
            Description = "Only the assigned au pair may upload batches.",
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

        var batchRequest = new CreateLocationLogBatchDto
        {
            DutyTaskId = duty.Id,
            Locations = new List<OfflineLocationPointDto>
            {
                new()
                {
                    Latitude = -26.2041,
                    Longitude = 28.0473,
                    CapturedAt = DateTimeOffset.UtcNow
                },
                new()
                {
                    Latitude = -26.2050,
                    Longitude = 28.0480,
                    CapturedAt = DateTimeOffset.UtcNow.AddMinutes(1)
                }
            }
        };

        // Act: the unauthorized au pair uploads a location batch.
        var result = await controller.LogLocationBatch(batchRequest);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: no locations were saved.
        Assert.Empty(_fixture.Context.LocationLogs);

        // Assert: no notification or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);

        // Assert: the duty remains InProgress.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);
    }
}