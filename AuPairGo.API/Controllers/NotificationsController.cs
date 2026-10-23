using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
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

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool TryGetAuthenticatedUserId(out int userId)
        {
            var userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(userIdValue, out userId);
        }

        [HttpPost("sos")]
        public async Task<IActionResult> SendSosAlert(
            [FromBody] SendSosAlertDto dto)
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
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetUserNotifications),
                new { userId = notification.RecipientUserId },
                notification.ToResponseDto());
        }

        [HttpPost]
        public async Task<IActionResult> SendNotification(
            [FromBody] SendNotificationDto dto)
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
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetUserNotifications),
                new { userId = notification.RecipientUserId },
                notification.ToResponseDto());
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(
            int userId,
            [FromQuery] bool unreadOnly = false)
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