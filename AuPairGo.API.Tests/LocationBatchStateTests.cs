using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationBatchStateTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationBatchStateTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Location_batch_cannot_be_uploaded_before_duty_check_in()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "batch-state-parent@test.com",
            FirstName = "Batch State",
            LastName = "Parent",
            CellNumber = "0000000140",
            Role = "Parent",
            OneSignalPlayerId = "batch-state-parent-device"
        };

        var auPairUser = new User
        {
            Email = "batch-state-aupair@test.com",
            FirstName = "Batch State",
            LastName = "Au Pair",
            CellNumber = "0000000141",
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
            Address = "Batch state test address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Batch state test au pair",
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
            StreetAddress = "270 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0015",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "280 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0015",
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
            Title = "Batch state test duty",
            Description = "Batch tracking must wait for check-in.",
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
                }
            }
        };

        // Act: upload a batch before the duty has started.
        var result = await controller.LogLocationBatch(batchRequest);

        // Assert: the API returns HTTP 409 Conflict.
        var response = Assert.IsType<ConflictObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            response.StatusCode);

        // Assert: no locations were saved.
        Assert.Empty(_fixture.Context.LocationLogs);

        // Assert: no notification or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);

        // Assert: the duty remains Pending.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.Pending, savedDuty.Status);
    }
}