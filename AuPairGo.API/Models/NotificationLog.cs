using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    public enum NotificationType
    {
        ScheduleUpdate,
        SosAlert,
        General,
        PickupArrival,
        ManualCheckIn
    }

    public enum PushDeliveryStatus
    {
        NotAttempted,
        Pending,
        Sent,
        Failed
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

        [Required]
        [Column("push_delivery_status")]
        public PushDeliveryStatus PushDeliveryStatus { get; set; } =
        PushDeliveryStatus.NotAttempted;

        [Column("push_sent_at")]
        public DateTime? PushSentAt { get; set; }

        [Column("push_error")]
        public string? PushError { get; set; }

        [Required]
        [Column("push_attempt_count")]
        public int PushAttemptCount { get; set; } = 0;

        [Column("push_last_attempt_at")]
        public DateTime? PushLastAttemptAt { get; set; }

        [Column("duty_task_id")]
        public int? DutyTaskId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}