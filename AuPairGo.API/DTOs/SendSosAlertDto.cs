using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class SendSosAlertDto
    {
        [Required(ErrorMessage = "SenderUserId is required.")]
        public int SenderUserId { get; set; }

        [Required(ErrorMessage = "RecipientUserId is required.")]
        public int RecipientUserId { get; set; }

        public int? ScheduleId { get; set; }

        [Required(ErrorMessage = "Alert message is required.")]
        [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
        public string Message { get; set; } = string.Empty;

        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
    }
}