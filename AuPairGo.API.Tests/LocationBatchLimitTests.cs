using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class LocationBatchLimitTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public LocationBatchLimitTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Batch_upload_rejects_more_than_500_location_points()
    {
        // Arrange: create 501 location points.
        var locations = new List<OfflineLocationPointDto>();

        for (var index = 0; index < 501; index++)
        {
            locations.Add(new OfflineLocationPointDto
            {
                Latitude = -26.2041,
                Longitude = 28.0473,
                CapturedAt = DateTimeOffset.UtcNow.AddMinutes(index)
            });
        }

        var batchRequest = new CreateLocationLogBatchDto
        {
            DutyTaskId = 999,
            Locations = locations
        };

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new LocationController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<LocationController>.Instance);

        // Arrange: provide a valid authenticated user claim.
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
                                "999")
                        },
                        "TestAuthentication"))
            }
        };

        // Act: submit the oversized batch.
        var result = await controller.LogLocationBatch(batchRequest);

        // Assert: the API returns HTTP 400 Bad Request.
        var response = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status400BadRequest,
            response.StatusCode);

        // Assert: no location logs were saved.
        Assert.Empty(_fixture.Context.LocationLogs);

        // Assert: no push was attempted.
        Assert.Empty(fakeOneSignal.Attempts);
    }
}