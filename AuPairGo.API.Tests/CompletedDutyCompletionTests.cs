using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class CompletedDutyCompletionTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public CompletedDutyCompletionTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Au_pair_cannot_complete_a_duty_twice()
    {
        // Arrange: create users.
        var parentUser = new User
        {
            Email = "completed-again-parent@test.com",
            FirstName = "Completed Again",
            LastName = "Parent",
            CellNumber = "0000000460",
            Role = "Parent",
            OneSignalPlayerId = "completed-again-parent-device"
        };

        var auPairUser = new User
        {
            Email = "completed-again-aupair@test.com",
            FirstName = "Completed Again",
            LastName = "Au Pair",
            CellNumber = "0000000461",
            Role = "AuPair",
            OneSignalPlayerId = "completed-again-aupair-device"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create profiles.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Completed again address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Completed again au pair",
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

        // Arrange: create required addresses.
        var pickupAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "390 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0021",
            latitude = -25.7479,
            longitude = 28.2293
        };

        var dropOffAddress = new Address
        {
            ParentProfileId = parentProfile.Id,
            StreetAddress = "400 Test Street",
            Suburb = "Pretoria",
            City = "Pretoria",
            PostalCode = "0021",
            latitude = -25.7485,
            longitude = 28.2300
        };

        _fixture.Context.AddRange(pickupAddress, dropOffAddress);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create an already completed duty.
        var duty = new DutyTask
        {
            ParentProfileId = parentProfile.Id,
            AuPairProfileId = auPairProfile.Id,
            PickupAddressId = pickupAddress.Id,
            DropOffAddressId = dropOffAddress.Id,
            Title = "Completed again test",
            Description = "A completed duty cannot be completed twice.",
            ScheduledStartTime = DateTime.UtcNow.AddHours(-2),
            ScheduledEndTime = DateTime.UtcNow.AddHours(-1),
            Status = DutyStatus.Completed
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

        // Act: attempt to complete the duty again.
        var result = await controller.CompleteDuty(duty.Id);

        // Assert: the API returns HTTP 409 Conflict.
        var response = Assert.IsType<ConflictObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            response.StatusCode);

        // Assert: the duty remains Completed.
        var savedDuty = await _fixture.Context.DutyTasks.FindAsync(duty.Id);

        Assert.NotNull(savedDuty);
        Assert.Equal(DutyStatus.Completed, savedDuty.Status);

        // Assert: no notification or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}