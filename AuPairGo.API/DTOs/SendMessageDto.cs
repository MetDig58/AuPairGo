using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class SendMessageDto
    {
        [Required(ErrorMessage = "SenderUserId is required.")]
        public int SenderUserId { get; set; }

        [Required(ErrorMessage = "RecipientUserId is required.")]
        public int RecipientUserId { get; set; }

        [Required(ErrorMessage = "MessageText is required.")]
        [StringLength(
            2000,
            MinimumLength = 1,
            ErrorMessage = "MessageText must be between 1 and 2000 characters.")]
        public string MessageText { get; set; } = string.Empty;
    }
}