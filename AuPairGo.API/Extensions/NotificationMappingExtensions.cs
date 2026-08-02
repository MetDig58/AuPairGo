using AuPairGo.API.DTOs;
using AuPairGo.API.Models;

namespace AuPairGo.API.Extensions
{
    public static class NotificationMappingExtensions
    {
        public static NotificationResponseDto ToResponseDto(this NotificationLog notification)
        {
            return new NotificationResponseDto
            {
                Id = notification.Id,
                RecipientUserId = notification.RecipientUserId,
                SenderUserId = notification.SenderUserId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                IsRead = notification.IsRead,
                ScheduleId = notification.ScheduleId,
                CreatedAt = notification.CreatedAt
            };
        }
    }
}