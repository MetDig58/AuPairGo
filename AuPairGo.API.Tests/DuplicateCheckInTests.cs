using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DuplicateCheckInTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DuplicateCheckInTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Au_pair_cannot_check_in_again_to_an_in_progress_duty()
    {
        // Arrange: create the parent and assigned au pair users.
        var parentUser = new User
        {
            Email = "duplicate-checkin-parent@test.com",
            FirstName = "Duplicate",
            LastName = "Check-in Parent",
            CellNumber = "0000000100",
            Role = "Parent",
            OneSignalPlayerId = "duplicate-checkin-parent-device"
        };

        var auPairUser = new User
        {
            Email = "duplicate-checkin-aupair@test.com",
            FirstName = "Duplicate",
            LastName = "Check-in Au Pair",
            CellNumber = "0000000101",
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
            Address = "Duplicate check-in address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Duplicate check-in test au pair",
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
            StreetAddress = "190 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0011",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "200 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0011",
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
            Title = "Duplicate check-in test",
            Description = "An au pair cannot check in twice.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.InProgress
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new DutiesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<DutiesController>.Instance);

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

        // Act: attempt to check in again.
        var result = await controller.CheckInToDuty(duty.Id);

        // Assert: the API returns HTTP 409 Conflict.
        var response = Assert.IsType<ConflictObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            response.StatusCode);

        // Assert: the duty remains InProgress.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);

        // Assert: the duplicate attempt created no notification or push.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}