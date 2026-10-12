using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuPairGo.API.Models
{
    [Table("chat_messages")]
    public class ChatMessage
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("sender_user_id")]
        public int SenderUserId { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public User? SenderUser { get; set; }

        [Required]
        [Column("recipient_user_id")]
        public int RecipientUserId { get; set; }

        [ForeignKey(nameof(RecipientUserId))]
        public User? RecipientUser { get; set; }

        [Required]
        [StringLength(2000)]
        [Column("message_text")]
        public string MessageText { get; set; } = string.Empty;

        [Column("sent_at")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }
    }
}