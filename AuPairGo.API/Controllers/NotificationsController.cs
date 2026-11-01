using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using AuPairGo.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOneSignalService _oneSignalService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(ApplicationDbContext context, IOneSignalService oneSignalService, ILogger<NotificationsController> logger)
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

        [HttpPost("sos")]
        public async Task<IActionResult> SendSosAlert([FromBody] SendSosAlertDto dto)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId == dto.RecipientUserId)
            {
                return BadRequest(
                    "A user cannot send an SOS notification to themselves.");
            }

            var recipientExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == dto.RecipientUserId);

            if (!recipientExists)
            {
                return NotFound(
                    $"User with ID {dto.RecipientUserId} was not found.");
            }

            var sosMessage = dto.Message;

            if (dto.CurrentLatitude.HasValue &&
                dto.CurrentLongitude.HasValue)
            {
                sosMessage +=
                    $" [GPS: {dto.CurrentLatitude.Value}, " +
                    $"{dto.CurrentLongitude.Value}]";
            }

            var notification = new NotificationLog
            {
                SenderUserId = authenticatedUserId,
                RecipientUserId = dto.RecipientUserId,
                ScheduleId = dto.ScheduleId,
                Title = "EMERGENCY SOS ALERT",
                Message = sosMessage,
                Type = NotificationType.SosAlert,
                IsRead = false,
                PushDeliveryStatus = PushDeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            await TryDeliverPushAsync(notification);

            return CreatedAtAction(
                            nameof(GetUserNotifications),
                            new { userId = notification.RecipientUserId },
                            notification.ToResponseDto());
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId == dto.RecipientUserId)
            {
                return BadRequest(
                    "A user cannot send a notification to themselves.");
            }

            var recipientExists = await _context.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == dto.RecipientUserId);

            if (!recipientExists)
            {
                return NotFound(
                    $"User with ID {dto.RecipientUserId} was not found.");
            }

            var notification = new NotificationLog
            {
                SenderUserId = authenticatedUserId,
                RecipientUserId = dto.RecipientUserId,
                ScheduleId = dto.ScheduleId,
                Title = dto.Title.Trim(),
                Message = dto.Message.Trim(),
                Type = dto.Type,
                IsRead = false,
                PushDeliveryStatus = PushDeliveryStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            await TryDeliverPushAsync(notification);

            return CreatedAtAction(
                            nameof(GetUserNotifications),
                            new { userId = notification.RecipientUserId },
                            notification.ToResponseDto());
        }

        private async Task TryDeliverPushAsync(NotificationLog notification)
        {
            var recipient = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(user =>
                    user.Id == notification.RecipientUserId);

            if (recipient == null)
            {
                notification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                notification.PushError = "Recipient user could not be found.";
                notification.PushLastAttemptAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogError(
                    "Notification {NotificationId} could not be delivered because recipient user {UserId} was not found.",
                    notification.Id,
                    notification.RecipientUserId);

                return;
            }

            if (string.IsNullOrWhiteSpace(recipient.OneSignalPlayerId))
            {
                notification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                notification.PushError =
                    "Recipient has no registered OneSignal subscription.";
                notification.PushLastAttemptAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "Notification {NotificationId} was saved, but recipient user {UserId} has no OneSignal subscription.",
                    notification.Id,
                    recipient.Id);

                return;
            }

            notification.PushDeliveryStatus = PushDeliveryStatus.Pending;
            notification.PushAttemptCount++;
            notification.PushLastAttemptAt = DateTime.UtcNow;
            notification.PushError = null;

            await _context.SaveChangesAsync();

            try
            {
                await _oneSignalService.SendPushNotificationAsync(
                    recipient.OneSignalPlayerId,
                    notification.Title,
                    notification.Message,
                    notification.Id,
                    notification.ScheduleId ?? 0);

                notification.PushDeliveryStatus = PushDeliveryStatus.Sent;
                notification.PushSentAt = DateTime.UtcNow;
                notification.PushError = null;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Notification {NotificationId} was delivered through OneSignal.",
                    notification.Id);
            }
            catch (HttpRequestException exception)
            {
                notification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                notification.PushError = exception.Message;
                notification.PushLastAttemptAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogError(
                    exception,
                    "Notification {NotificationId} was saved, but OneSignal delivery failed.",
                    notification.Id);
            }
            catch (InvalidOperationException exception)
            {
                notification.PushDeliveryStatus = PushDeliveryStatus.Failed;
                notification.PushError = exception.Message;
                notification.PushLastAttemptAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogError(
                    exception,
                    "Notification {NotificationId} was saved, but OneSignal configuration is missing.",
                    notification.Id);
            }
        }

        [HttpPut("device")]
        public async Task<IActionResult> UpdateOneSignalDevice([FromBody] UpdateOneSignalDeviceDto dto)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(existingUser =>
                    existingUser.Id == authenticatedUserId);

            if (user == null)
            {
                return NotFound(
                    $"User with ID {authenticatedUserId} was not found.");
            }

            user.OneSignalPlayerId = dto.OneSignalPlayerId.Trim();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "OneSignal device registered successfully."
            });
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int userId, [FromQuery] bool unreadOnly = false)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            if (authenticatedUserId != userId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "You can only view your own notifications.");
            }

            var query = _context.NotificationLogs
                .AsNoTracking()
                .Where(notification =>
                    notification.RecipientUserId == authenticatedUserId);

            if (unreadOnly)
            {
                query = query.Where(notification =>
                    !notification.IsRead);
            }

            var notifications = await query
                .OrderByDescending(notification =>
                    notification.CreatedAt)
                .ToListAsync();

            return Ok(notifications
                .Select(notification =>
                    notification.ToResponseDto())
                .ToList());
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!TryGetAuthenticatedUserId(
                    out var authenticatedUserId))
            {
                return Unauthorized(
                    "The authenticated user ID is missing or invalid.");
            }

            var notification = await _context.NotificationLogs
                .FirstOrDefaultAsync(notification =>
                    notification.Id == id);

            if (notification == null)
            {
                return NotFound(
                    $"Notification with ID {id} was not found.");
            }

            if (notification.RecipientUserId != authenticatedUserId)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "Only the notification recipient can mark it as read.");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Ok(notification.ToResponseDto());
        }
    }
}