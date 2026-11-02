using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DutyCompletionAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DutyCompletionAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Unassigned_au_pair_cannot_complete_someone_elses_duty()
    {
        // Arrange: create parent, assigned au pair, and unauthorized au pair.
        var parentUser = new User
        {
            Email = "completion-auth-parent@test.com",
            FirstName = "Completion Auth",
            LastName = "Parent",
            CellNumber = "0000000470",
            Role = "Parent",
            OneSignalPlayerId = "completion-auth-parent-device"
        };

        var assignedAuPairUser = new User
        {
            Email = "completion-auth-assigned@test.com",
            FirstName = "Assigned",
            LastName = "Au Pair",
            CellNumber = "0000000471",
            Role = "AuPair"
        };

        var unauthorizedAuPairUser = new User
        {
            Email = "completion-auth-unauthorized@test.com",
            FirstName = "Unauthorized",
            LastName = "Au Pair",
            CellNumber = "0000000472",
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
            Address = "Completion authorization address"
        };

        var assignedAuPairProfile = new AuPairProfile
        {
            UserId = assignedAuPairUser.Id,
            Bio = "Assigned completion au pair",
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
            Bio = "Unauthorized completion au pair",
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

        // Arrange: create the required addresses.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "410 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0022",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "420 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0022",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: assign an active duty to the assigned au pair.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = assignedAuPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Completion authorization test",
            Description = "Only the assigned au pair may complete this duty.",
            ScheduledStartTime = DateTime.UtcNow.AddHours(-1),
            ScheduledEndTime = DateTime.UtcNow.AddHours(1),
            Status = DutyStatus.InProgress
        };

        _fixture.Context.DutyTasks.Add(duty);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new DutiesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<DutiesController>.Instance);

        // Arrange: authenticate as the unauthorized au pair.
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

        // Act: attempt to complete another au pair's duty.
        var result = await controller.CompleteDuty(duty.Id);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: the duty remains InProgress.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.InProgress, savedDuty.Status);

        // Assert: no notification or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}