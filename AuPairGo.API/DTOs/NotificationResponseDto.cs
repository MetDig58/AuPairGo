namespace AuPairGo.API.DTOs
{
    public class NotificationResponseDto
    {
        public int Id { get; set; }
        public int RecipientUserId { get; set; }
        public int SenderUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}