using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class InvalidAuthenticationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public InvalidAuthenticationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Device_registration_returns_unauthorized_without_user_id_claim()
    {
        // Arrange: create a controller with an authenticated-looking
        // request that does not contain the required user ID claim.
        var fakeOneSignal = new FakeOneSignalService();

        var controller = new NotificationsController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<NotificationsController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var request = new UpdateOneSignalDeviceDto
        {
            OneSignalPlayerId = "should-not-be-saved"
        };

        // Act: attempt device registration without a user ID claim.
        var result = await controller.UpdateOneSignalDevice(request);

        // Assert: the API returns HTTP 401 Unauthorized.
        var response = Assert.IsType<UnauthorizedObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status401Unauthorized,
            response.StatusCode);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}