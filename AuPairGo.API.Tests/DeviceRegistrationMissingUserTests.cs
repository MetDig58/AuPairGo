using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DeviceRegistrationMissingUserTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DeviceRegistrationMissingUserTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Device_registration_returns_not_found_for_missing_authenticated_user()
    {
        // Arrange: create one real user.
        var existingUser = new User
        {
            Email = "existing-device-user@test.com",
            FirstName = "Existing",
            LastName = "User",
            CellNumber = "0000000250",
            Role = "Parent",
            OneSignalPlayerId = "existing-device"
        };

        _fixture.Context.Users.Add(existingUser);
        await _fixture.Context.SaveChangesAsync();

        var missingUserId = existingUser.Id + 1000;

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate with a user ID that is not in the database.
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
                                missingUserId.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new UpdateOneSignalDeviceDto
        {
            OneSignalPlayerId = "should-not-be-saved"
        };

        // Act: attempt device registration.
        var result = await controller.UpdateOneSignalDevice(request);

        // Assert: the API returns HTTP 404 Not Found.
        var response = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            response.StatusCode);

        // Assert: the real user was not modified.
        var savedUser = await _fixture.Context.Users.FindAsync(existingUser.Id);

        Assert.NotNull(savedUser);
        Assert.Equal(
            "existing-device",
            savedUser.OneSignalPlayerId);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}