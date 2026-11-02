using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DeviceRegistrationAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DeviceRegistrationAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Device_registration_updates_only_the_authenticated_user()
    {
        // Arrange: create two users with existing device IDs.
        var authenticatedUser = new User
        {
            Email = "device-authenticated@test.com",
            FirstName = "Authenticated",
            LastName = "Device User",
            CellNumber = "0000000240",
            Role = "Parent",
            OneSignalPlayerId = "authenticated-old-device"
        };

        var otherUser = new User
        {
            Email = "device-other@test.com",
            FirstName = "Other",
            LastName = "Device User",
            CellNumber = "0000000241",
            Role = "AuPair",
            OneSignalPlayerId = "other-existing-device"
        };

        _fixture.Context.Users.AddRange(
            authenticatedUser,
            otherUser);

        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as the first user.
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
                                authenticatedUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new UpdateOneSignalDeviceDto
        {
            OneSignalPlayerId = "authenticated-new-device"
        };

        // Act: register a device while authenticated as the first user.
        var result = await controller.UpdateOneSignalDevice(request);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: only the authenticated user's device changed.
        var savedAuthenticatedUser =
            await _fixture.Context.Users.FindAsync(
                authenticatedUser.Id);

        var savedOtherUser =
            await _fixture.Context.Users.FindAsync(otherUser.Id);

        Assert.NotNull(savedAuthenticatedUser);
        Assert.NotNull(savedOtherUser);

        Assert.Equal(
            "authenticated-new-device",
            savedAuthenticatedUser.OneSignalPlayerId);

        Assert.Equal(
            "other-existing-device",
            savedOtherUser.OneSignalPlayerId);

        // Assert: device registration does not send a push.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}