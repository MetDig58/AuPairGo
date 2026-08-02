using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public enum NotificationType
    {
        ScheduleUpdate,
        SosAlert,
        General
    }

    [Table("notification_logs")]
    public class NotificationLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("recipient_user_id")]
        public int RecipientUserId { get; set; }

        [Required]
        [Column("sender_user_id")]
        public int SenderUserId { get; set; }

        [Required]
        [Column("title")]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("notification_type")]
        public NotificationType Type { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("schedule_id")]
        public int? ScheduleId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}