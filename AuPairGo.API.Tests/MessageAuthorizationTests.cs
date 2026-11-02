using System.Security.Claims;
using AuPairGo.API.Controllers;
using AuPairGo.API.DTOs;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuPairGo.API.Tests;

public class MessageAuthorizationTests
    : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public MessageAuthorizationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Parent_and_au_pair_cannot_message_without_an_eligible_duty()
    {
        // Arrange: create the parent and au pair users.
        var parentUser = new User
        {
            Email = "message-auth-parent@test.com",
            FirstName = "Message Auth",
            LastName = "Parent",
            CellNumber = "0000000270",
            Role = "Parent",
            OneSignalPlayerId = "message-auth-parent-device"
        };

        var auPairUser = new User
        {
            Email = "message-auth-aupair@test.com",
            FirstName = "Message Auth",
            LastName = "Au Pair",
            CellNumber = "0000000271",
            Role = "AuPair",
            OneSignalPlayerId = "message-auth-aupair-device"
        };

        _fixture.Context.Users.AddRange(parentUser, auPairUser);
        await _fixture.Context.SaveChangesAsync();

        // Arrange: create profiles, but do not create a qualifying duty.
        var parentProfile = new ParentProfile
        {
            UserId = parentUser.Id,
            NumberOfChildren = 1,
            ChildAgeGroups = "School age",
            Address = "Message authorization address"
        };

        var auPairProfile = new AuPairProfile
        {
            UserId = auPairUser.Id,
            Bio = "Message authorization test au pair",
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

        var fakeOneSignal = new FakeOneSignalService();

        var controller = new MessagesController(
            _fixture.Context,
            fakeOneSignal,
            NullLogger<MessagesController>.Instance);

        // Arrange: authenticate as the parent.
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
                                parentUser.Id.ToString())
                        },
                        "TestAuthentication"))
            }
        };

        var request = new SendMessageDto
        {
            RecipientUserId = auPairUser.Id,
            MessageText = "This message should be rejected."
        };

        // Act: attempt to message without an eligible duty.
        var result = await controller.SendMessage(request);

        // Assert: the API returns HTTP 403 Forbidden.
        var response = Assert.IsType<ObjectResult>(result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            response.StatusCode);

        // Assert: no chat message was saved.
        Assert.Empty(_fixture.Context.ChatMessages);

        // Assert: no notification or push was created.
        Assert.Empty(_fixture.Context.NotificationLogs);
        Assert.Empty(fakeOneSignal.Attempts);
    }
}