using System.ComponentModel.DataAnnotations;
using AuPairGo.API.Models;

namespace AuPairGo.API.DTOs
{
    public class SendNotificationDto
    {
        [Required(ErrorMessage = "RecipientUserId is required.")]
        public int RecipientUserId { get; set; }

        public int? ScheduleId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required.")]
        [StringLength(1000, ErrorMessage = "Message cannot exceed 1000 characters.")]
        public string Message { get; set; } = string.Empty;

        public NotificationType Type { get; set; } = NotificationType.ScheduleUpdate;
    }
}