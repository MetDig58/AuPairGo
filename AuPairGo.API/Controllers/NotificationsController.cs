using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        //POST: api/notifications/sos
        [HttpPost("sos")]
        public async Task<IActionResult> SendSosAlert([FromBody] SendSosAlertDto dto)
        {
            var SosMessage = dto.Message;
            if (dto.CurrentLatitude.HasValue && dto.CurrentLongitude.HasValue)
            {
                SosMessage += $" [GPS: {dto.CurrentLatitude.Value}, {dto.CurrentLongitude.Value}]";
            }

            var Notification = new NotificationLog
            {
                SenderUserId = dto.SenderUserId,
                RecipientUserId = dto.RecipientUserId,
                ScheduleId = dto.ScheduleId,
                Title = "🚨 EMERGENCY SOS ALERT",
                Message = SosMessage,
                Type = NotificationType.SosAlert,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(Notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetUserNotifications),
                new { userId = Notification.RecipientUserId },
                Notification.ToResponseDto()
            );
        }

        // POST: api/notifications 
        [HttpPost]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            var Notification = new NotificationLog
            {
                SenderUserId = dto.SenderUserId,
                RecipientUserId = dto.RecipientUserId,
                ScheduleId = dto.ScheduleId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(Notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetUserNotifications),
                new { userId = Notification.RecipientUserId },
                Notification.ToResponseDto()
            );
        }

        //GET: api/notifications/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(int UserId, [FromQuery] bool UnreadOnly = false)
        {
            var Query = _context.NotificationLogs
                .AsNoTracking()
                .Where(Notifications => Notifications.RecipientUserId == UserId);

            if (UnreadOnly)
            {
                Query = Query.Where(Notifications => !Notifications.IsRead);
            }

            var Notifications = await Query
                .OrderByDescending(Notifications => Notifications.CreatedAt)
                .Select(Notifications => Notifications.ToResponseDto())
                .ToListAsync();

            return Ok(Notifications);
        }

        //PATCH: api/notifications/{id}/read
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int Id)
        {
            var Notification = await _context.NotificationLogs.FindAsync(Id);
            if (Notification == null)
            {
                return NotFound($"Notification with ID {Id} was not found.");
            }

            Notification.IsRead = true;
            await _context.SaveChangesAsync();

            return Ok(Notification.ToResponseDto());
        }
    }
}