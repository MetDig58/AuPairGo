namespace AuPairGo.API.DTOs
{
    public class ChatMessageResponseDto
    {
        public int Id { get; set; }
        public int SenderUserId { get; set; }
        public int RecipientUserId { get; set; }
        public string MessageText { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}