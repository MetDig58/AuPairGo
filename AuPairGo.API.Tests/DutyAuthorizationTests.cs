using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DutyAuthorizationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DutyAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Unassigned_au_pair_cannot_check_in_to_someone_elses_duty()
    {
        // Arrange: create the parent, assigned au pair, and unauthorized au pair.
        var parentUser = new User
        {
            Email = "parent-auth-test@test.com",
            FirstName = "Test",
            LastName = "Parent",
            CellNumber = "0000000010",
            Role = "Parent",
            OneSignalPlayerId = "parent-auth-test-device"
        };

        var assignedAuPairUser = new User
        {
            Email = "assigned-aupair-auth-test@test.com",
            FirstName = "Assigned",
            LastName = "Au Pair",
            CellNumber = "0000000011",
            Role = "AuPair"
        };

        var unauthorizedAuPairUser = new User
        {
            Email = "unauthorized-aupair-auth-test@test.com",
            FirstName = "Unauthorized",
            LastName = "Au Pair",
            CellNumber = "0000000012",
            Role = "AuPair"
        };

        _fixture.Context.Users.AddRange(
            parentUser,
            assignedAuPairUser,
            unauthorizedAuPairUser);

        await _fixture.Context.SaveChangesAsync();

        // Arrange: create the parent and both au pair profiles.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Test parent address"
        };

        var assignedAuPairProfile = new AuPairProfile
        {
            UserId = assignedAuPairUser.Id,
            Bio = "Assigned test au pair",
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
            Bio = "Unauthorized test au pair",
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
            StreetAddress = "10 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0002",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "20 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0002",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: assign the duty only to the assigned au pair.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = assignedAuPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Authorization test duty",
            Description = "This duty belongs to the assigned au pair.",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(2),
            Status = DutyStatus.Pending
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new DutiesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<DutiesController>.Instance);

        // Arrange: authenticate as the au pair who is NOT assigned to the duty.
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

        // Act: the unauthorized au pair attempts to check in.
        var result = await controller.CheckInToDuty(duty.Id);

        // Assert: the request is rejected with HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);

        // Assert: the duty remains Pending.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.Pending, savedDuty.Status);

        // Assert: no notification or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}