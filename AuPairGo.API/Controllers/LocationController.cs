using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AuPairGo.API.Services;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOneSignalService _oneSignalService;
        private readonly ILogger<LocationController> _logger;

        public LocationController(ApplicationDbContext context, IOneSignalService oneSignalService, ILogger<LocationController> logger)
        {
            _context = context;
            _oneSignalService = oneSignalService;
            _logger = logger;
        }

        private bool TryGetAuthenticatedUserId(out int userId)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userIdValue, out userId);
        }

        private static bool IsDutyParticipant(DutyTask dutyTask, int authenticatedUserId)
        {
            return dutyTask.ParentProfile?.UserId ==
                       authenticatedUserId
                   || dutyTask.AuPairProfile?.UserId ==
                       authenticatedUserId;
        }

        private static bool IsAssignedAuPair(DutyTask dutyTask, int authenticatedUserId)
        {
            return dutyTask.AuPairProfile?.UserId == authenticatedUserId;
        }

        private static double CalculateDistanceInMeters(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            const double earthRadiusInMeters = 6_371_000;

            var latitude1Radians = latitude1 * Math.PI / 180;
            var latitude2Radians = latitude2 * Math.PI / 180;
            var latitudeDifferenceRadians =
                (latitude2 - latitude1) * Math.PI / 180;
            var longitudeDifferenceRadians =
                (longitude2 - longitude1) * Math.PI / 180;

            var haversine =
                Math.Sin(latitudeDifferenceRadians / 2) *
                Math.Sin(latitudeDifferenceRadians / 2) +
                Math.Cos(latitude1Radians) *
                Math.Cos(latitude2Radians) *
                Math.Sin(longitudeDifferenceRadians / 2) *
                Math.Sin(longitudeDifferenceRadians / 2);

            var angularDistance =
                2 * Math.Atan2(
                    Math.Sqrt(haversine),
                    Math.Sqrt(1 - haversine));

            return earthRadiusInMeters * angularDistance;
        }

        [HttpPost("logs")]
        public async Task<IActionResult> LogLocation([FromBody] CreateLocationLogDto dto)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .Include(duty => duty.PickupAddress)
                .FirstOrDefaultAsync(duty =>
                        duty.Id == dto.DutyTaskId);

            if (dutyTask == null)
            {
                return NotFound(
                    $"Duty task with ID {dto.DutyTaskId} was not found.");
            }

            if (!IsAssignedAuPair(dutyTask, authenticatedUserId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned au pair can submit location for this duty.");
            }

            if (dutyTask.Status != DutyStatus.InProgress)
            {
                return Conflict(
                    "Location tracking is only allowed while the duty is in progress.");
            }

            var locationLog = new LocationLog
            {
                DutyTaskId = dutyTask.Id,
                ChildId = dutyTask.ChildProfileId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                TimeStamp = DateTime.UtcNow
            };

            _context.LocationLogs.Add(locationLog);

            NotificationLog? arrivalNotification = null;

            var pickupAddress = dutyTask.PickupAddress;

            if (!dutyTask.PickupArrivalNotified &&
                pickupAddress is not null)
            {
                var distanceToPickup = CalculateDistanceInMeters(
                    dto.Latitude,
                    dto.Longitude,
                    pickupAddress.latitude,
                    pickupAddress.longitude);

                if (distanceToPickup <= 100)
                {
                    var parentUserId = dutyTask.ParentProfile?.UserId;
                    var auPairUserId = dutyTask.AuPairProfile?.UserId;

                    if (parentUserId.HasValue && auPairUserId.HasValue)
                    {
                        arrivalNotification = new NotificationLog
                        {
                            RecipientUserId = parentUserId.Value,
                            SenderUserId = auPairUserId.Value,
                            ScheduleId = dutyTask.Id,
                            Title = "Au Pair has arrived",
                            Message =
                                $"The au pair has arrived within 100 metres of the pickup point for duty '{dutyTask.Title}'.",
                            Type = NotificationType.PickupArrival,
                            IsRead = false,
                            PushDeliveryStatus = PushDeliveryStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.NotificationLogs.Add(arrivalNotification);

                        dutyTask.PickupArrivalNotified = true;
                        dutyTask.PickupArrivalNotifiedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (arrivalNotification != null)
            {
                var parentUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user =>
                        user.Id == arrivalNotification.RecipientUserId);

                if (parentUser == null)
                {
                    arrivalNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                    arrivalNotification.PushError = "Recipient user could not be found.";

                    await _context.SaveChangesAsync();

                    _logger.LogError(
                        "Arrival notification {NotificationId} was saved, but recipient user {UserId} was not found.",
                        arrivalNotification.Id,
                        arrivalNotification.RecipientUserId);

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "The arrival notification was saved, but its recipient could not be found.");
                }

                if (string.IsNullOrWhiteSpace(parentUser.OneSignalPlayerId))
                {
                    arrivalNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                    arrivalNotification.PushError =
                        "Parent user has no registered OneSignal subscription.";

                    await _context.SaveChangesAsync();

                    _logger.LogWarning(
                        "Arrival notification {NotificationId} was saved, but parent user {UserId} has no OneSignal subscription registered.",
                        arrivalNotification.Id,
                        parentUser.Id);

                    return Conflict(
                        "The arrival notification was saved, but the parent has no registered OneSignal device.");
                }

                try
                {
                    await _oneSignalService.SendPushNotificationAsync(
                        parentUser.OneSignalPlayerId,
                        arrivalNotification.Title,
                        arrivalNotification.Message,
                        arrivalNotification.Id,
                        dutyTask.Id);
                    arrivalNotification.PushDeliveryStatus = PushDeliveryStatus.Sent;
                    arrivalNotification.PushSentAt = DateTime.UtcNow;
                    arrivalNotification.PushError = null;

                    await _context.SaveChangesAsync();
                }
                catch (HttpRequestException exception)
                {
                    arrivalNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                    arrivalNotification.PushError = exception.Message;

                    await _context.SaveChangesAsync();
                    _logger.LogError(
                        exception,
                        "Arrival notification {NotificationId} was saved, but OneSignal delivery failed.",
                        arrivalNotification.Id);

                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        "The arrival notification was saved, but OneSignal delivery failed.");
                }
                catch (InvalidOperationException exception)
                {
                    arrivalNotification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                    arrivalNotification.PushError = exception.Message;

                    await _context.SaveChangesAsync();
                    _logger.LogError(
                        exception,
                        "Arrival notification {NotificationId} was saved, but OneSignal configuration is missing.",
                        arrivalNotification.Id);

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "The arrival notification was saved, but OneSignal is not configured.");
                }
            }

            return CreatedAtAction(
                                    nameof(GetLocationLogsForDuty),
                                    new { dutyTaskId = locationLog.DutyTaskId },
                                    locationLog.ToResponseDto());
        }

        [HttpPost("logs/batch")]
        public async Task<IActionResult> LogLocationBatch(
    [FromBody] CreateLocationLogBatchDto dto)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            if (dto.Locations.Count > 500)
            {
                return BadRequest(
                    "A maximum of 500 location points can be uploaded at once.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .Include(duty => duty.PickupAddress)
                .FirstOrDefaultAsync(duty =>
                    duty.Id == dto.DutyTaskId);

            if (dutyTask == null)
            {
                return NotFound(
                    $"Duty task with ID {dto.DutyTaskId} was not found.");
            }

            if (!IsAssignedAuPair(dutyTask, authenticatedUserId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned au pair can submit location for this duty.");
            }

            if (dutyTask.Status != DutyStatus.InProgress)
            {
                return Conflict(
                    "Location tracking is only allowed while the duty is in progress.");
            }

            var orderedPoints = dto.Locations
                .OrderBy(point => point.CapturedAt)
                .ToList();

            var locationLogs = orderedPoints
                .Select(point => new LocationLog
                {
                    DutyTaskId = dutyTask.Id,
                    ChildId = dutyTask.ChildProfileId,
                    Latitude = point.Latitude,
                    Longitude = point.Longitude,
                    TimeStamp = point.CapturedAt.UtcDateTime
                })
                .ToList();

            _context.LocationLogs.AddRange(locationLogs);

            NotificationLog? arrivalNotification = null;

            if (!dutyTask.PickupArrivalNotified &&
                dutyTask.PickupAddress is not null)
            {
                var pickupAddress = dutyTask.PickupAddress;

                var arrivedPoint = orderedPoints.FirstOrDefault(point =>
                    CalculateDistanceInMeters(
                        point.Latitude,
                        point.Longitude,
                        pickupAddress.latitude,
                        pickupAddress.longitude) <= 100);

                if (arrivedPoint is not null)
                {
                    var parentUserId = dutyTask.ParentProfile?.UserId;
                    var auPairUserId = dutyTask.AuPairProfile?.UserId;

                    if (parentUserId.HasValue && auPairUserId.HasValue)
                    {
                        arrivalNotification = new NotificationLog
                        {
                            RecipientUserId = parentUserId.Value,
                            SenderUserId = auPairUserId.Value,
                            ScheduleId = dutyTask.Id,
                            Title = "Au Pair has arrived",
                            Message =
                                $"The au pair has arrived within 100 metres of the pickup point for duty '{dutyTask.Title}'.",
                            Type = NotificationType.PickupArrival,
                            IsRead = false,
                            PushDeliveryStatus = PushDeliveryStatus.Pending,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.NotificationLogs.Add(arrivalNotification);

                        dutyTask.PickupArrivalNotified = true;
                        dutyTask.PickupArrivalNotifiedAt = DateTime.UtcNow;
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (arrivalNotification != null)
            {
                var parentUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(user =>
                        user.Id == arrivalNotification.RecipientUserId);

                if (parentUser == null)
                {
                    arrivalNotification.PushDeliveryStatus =
                        PushDeliveryStatus.Failed;
                    arrivalNotification.PushError =
                        "Recipient user could not be found.";

                    await _context.SaveChangesAsync();

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "The locations were saved, but the notification recipient could not be found.");
                }

                if (string.IsNullOrWhiteSpace(parentUser.OneSignalPlayerId))
                {
                    arrivalNotification.PushDeliveryStatus =
                        PushDeliveryStatus.Failed;
                    arrivalNotification.PushError =
                        "Parent user has no registered OneSignal subscription.";

                    await _context.SaveChangesAsync();

                    return Conflict(
                        "The locations were saved, but the parent has no registered OneSignal device.");
                }

                try
                {
                    await _oneSignalService.SendPushNotificationAsync(
                        parentUser.OneSignalPlayerId,
                        arrivalNotification.Title,
                        arrivalNotification.Message,
                        arrivalNotification.Id,
                        dutyTask.Id);

                    arrivalNotification.PushDeliveryStatus =
                        PushDeliveryStatus.Sent;
                    arrivalNotification.PushSentAt = DateTime.UtcNow;
                    arrivalNotification.PushError = null;

                    await _context.SaveChangesAsync();
                }
                catch (HttpRequestException exception)
                {
                    arrivalNotification.PushDeliveryStatus =
                        PushDeliveryStatus.Failed;
                    arrivalNotification.PushError = exception.Message;

                    await _context.SaveChangesAsync();

                    _logger.LogError(
                        exception,
                        "Batch arrival notification {NotificationId} delivery failed.",
                        arrivalNotification.Id);

                    return StatusCode(
                        StatusCodes.Status502BadGateway,
                        "The locations were saved, but OneSignal delivery failed.");
                }
                catch (InvalidOperationException exception)
                {
                    arrivalNotification.PushDeliveryStatus =
                        PushDeliveryStatus.Failed;
                    arrivalNotification.PushError = exception.Message;

                    await _context.SaveChangesAsync();

                    _logger.LogError(
                        exception,
                        "Batch arrival notification {NotificationId} configuration is missing.",
                        arrivalNotification.Id);

                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        "The locations were saved, but OneSignal is not configured.");
                }
            }

            return Ok(new
            {
                dutyTaskId = dutyTask.Id,
                acceptedCount = locationLogs.Count,
                arrivalNotificationId = arrivalNotification?.Id
            });
        }

        [HttpGet("duties/{dutyTaskId}")]
        public async Task<IActionResult> GetLocationLogsForDuty(int dutyTaskId)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var dutyTask = await _context.DutyTasks
                .Include(duty => duty.ParentProfile)
                .Include(duty => duty.AuPairProfile)
                .AsNoTracking()
                .FirstOrDefaultAsync(duty =>
                    duty.Id == dutyTaskId);

            if (dutyTask == null)
            {
                return NotFound(
                    $"Duty task with ID {dutyTaskId} was not found.");
            }

            if (!IsDutyParticipant(
                    dutyTask,
                    authenticatedUserId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the assigned parent or au pair can view location for this duty.");
            }

            var logs = await _context.LocationLogs
                .AsNoTracking()
                .Where(location =>
                    location.DutyTaskId == dutyTaskId)
                .OrderByDescending(location =>
                    location.TimeStamp)
                .ToListAsync();

            return Ok(logs
                .Select(location => location.ToResponseDto())
                .ToList());
        }
    }
}