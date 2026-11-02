using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class DeviceRegistrationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public DeviceRegistrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task User_can_register_a_OneSignal_device()
    {
        // Arrange: create a user without a registered device.
        var user = new User
        {
            Email = "device-registration@test.com",
            FirstName = "Device",
            LastName = "Registration",
            CellNumber = "0000000230",
            Role = "Parent",
            OneSignalPlayerId = string.Empty
        };

        _fixture.Context.Users.Add(user);
        await _fixture.Context.SaveChangesAsync();

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        // Arrange: authenticate as that user.
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
                                user.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new UpdateOneSignalDeviceDto
        {
            OneSignalPlayerId = "new-device-subscription-123"
        };

        // Act: register the device.
        var result = await controller.UpdateOneSignalDevice(request);

        // Assert: the API returns HTTP 200 OK.
        var response = Assert.IsType<OkObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status200OK,
            response.StatusCode);

        // Assert: the device ID was saved to the user.
        var savedUser = await _fixture.Context.Users.FindAsync(user.Id);

        Assert.NotNull(savedUser);
        Assert.Equal(
            "new-device-subscription-123",
            savedUser.OneSignalPlayerId);

        // Assert: device registration does not send a push.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}