using AuPairGo.API.DTOs;
using AuPairGo.API.Models;

namespace AuPairGo.API.Extensions
{
    public static class ChatMappingExtensions
    {
        public static ChatMessageResponseDto ToResponseDto(this ChatMessage message)
        {
            return new ChatMessageResponseDto
            {
                Id = message.Id,
                SenderUserId = message.SenderUserId,
                RecipientUserId = message.RecipientUserId,
                MessageText = message.MessageText,
                SentAt = message.SentAt,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt
            };
        }
    }
}