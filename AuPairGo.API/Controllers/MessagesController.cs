using AuPairGo.API.Data;
using AuPairGo.API.DTOs;
using AuPairGo.API.Extensions;
using AuPairGo.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuPairGo.API.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageDto dto)
        {
            if (dto.SenderUserId == dto.RecipientUserId)
            {
                return BadRequest("A user cannot send a message to themselves.");
            }

            var users = await _context.Users
                .AsNoTracking()
                .Where(user => user.Id == dto.SenderUserId ||
                               user.Id == dto.RecipientUserId)
                .Select(user => new
                {
                    user.Id,
                    user.Role
                })
                .ToListAsync();

            var sender = users.FirstOrDefault(user => user.Id == dto.SenderUserId);
            var recipient = users.FirstOrDefault(user => user.Id == dto.RecipientUserId);

            if (sender == null || recipient == null)
            {
                return NotFound("The sender or recipient user was not found.");
            }

            var canMessage = await CanUsersMessageAsync(
                dto.SenderUserId,
                sender.Role,
                dto.RecipientUserId,
                recipient.Role);

            if (!canMessage)
            {
                return Forbid("These users are not permitted to message each other.");
            }

            var message = new ChatMessage
            {
                SenderUserId = dto.SenderUserId,
                RecipientUserId = dto.RecipientUserId,
                MessageText = dto.MessageText.Trim(),
                SentAt = DateTime.UtcNow,
                IsRead = false,
                ReadAt = null
            };

            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message.ToResponseDto());
        }

        private async Task<bool> CanUsersMessageAsync(
            int senderUserId,
            string senderRole,
            int recipientUserId,
            string recipientRole)
        {
            var rolesAreParentAndAuPair =
                (senderRole == "Parent" && recipientRole == "AuPair") ||
                (senderRole == "AuPair" && recipientRole == "Parent");

            if (senderRole == "Parent" && recipientRole == "Parent")
            {
                return true;
            }

            if (!rolesAreParentAndAuPair)
            {
                return false;
            }

            var parentUserId = senderRole == "Parent"
                ? senderUserId
                : recipientUserId;

            var auPairUserId = senderRole == "AuPair"
                ? senderUserId
                : recipientUserId;

            return await _context.DutyTasks
                .AsNoTracking()
                .AnyAsync(duty =>
                    duty.ParentProfile != null &&
                    duty.ParentProfile.UserId == parentUserId &&
                    duty.AuPairProfile != null &&
                    duty.AuPairProfile.UserId == auPairUserId &&
                    duty.ChildProfileId.HasValue &&
                    duty.Status != DutyStatus.Cancelled);
        }

        [HttpGet("conversation/{user1Id}/{user2Id}")]
        public async Task<IActionResult> GetConversations(int user1Id, int user2Id)
        {
            if (user1Id == user2Id)
            {
                return BadRequest("A conversation requires 2 different users.");
            }

            var users = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == user1Id || user.Id == user2Id)
            .Select(user => new
            {
                user.Id,
                user.Role
            })
            .ToListAsync();

            var user1 = users.FirstOrDefault(user => user.Id == user1Id);
            var user2 = users.FirstOrDefault(user => user.Id == user2Id);

            if (user1 == null || user2 == null)
            {
                return NotFound("One or both users were not found.");
            }

            var canMessage = await CanUsersMessageAsync(
                user1Id,
                user1.Role,
                user2Id,
                user2.Role);

            if (!canMessage)
            {
                return Forbid("These users are not allowed to message each other.");
            }

            var messages = await _context.ChatMessages
        .AsNoTracking()
        .Where(message =>
            (message.SenderUserId == user1Id &&
            message.RecipientUserId == user2Id) ||
            (message.SenderUserId == user2Id &&
            message.RecipientUserId == user1Id))
        .OrderBy(message => message.SentAt)
        .ToListAsync();

            return Ok(messages.Select(message => message.ToResponseDto()).ToList());
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkMessagesAsRead(int id)
        {
            var message = await _context.ChatMessages
            .FirstOrDefaultAsync(message => message.Id == id);

            if (message == null)
            {
                return NotFound($"Message with ID {id} was not found.");
            }

            if (message.IsRead)
            {
                return Ok(message.ToResponseDto());
            }

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(message.ToResponseDto());
        }

        [HttpGet("recent/{userId}")]
        public async Task<IActionResult> GetRecentConversations(int userId)
        {
            var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId);

            if (!userExists)
            {
                return NotFound($"User with ID {userId} was not found.");
            }

            var messageSummaries = await _context.ChatMessages
            .AsNoTracking()
            .Where(message =>
            message.SenderUserId == userId ||
            message.RecipientUserId == userId)
            .OrderByDescending(message => message.SentAt)
            .Select(message => new
            {
                OtherUserId = message.SenderUserId == userId
                ? message.RecipientUserId
                : message.SenderUserId,
                message.MessageText,
                message.SentAt,
                message.IsRead
            })
            .ToListAsync();

            var recentConversations = messageSummaries
            .GroupBy(message => message.OtherUserId)
            .Select(group => group.First())
            .ToList();

            var otherUserIds = recentConversations
            .Select(conversation => conversation.OtherUserId)
            .ToList();

            var users = await _context.Users
            .AsNoTracking()
            .Where(user => otherUserIds.Contains(user.Id))
            .Select(user => new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Role
            })
            .ToDictionaryAsync(user => user.Id);

            var response = recentConversations
            .Where(conversation => users.ContainsKey(conversation.OtherUserId))
            .Select(conversation =>
            {
                var otherUser = users[conversation.OtherUserId];

                return new
                {
                    otherUserId = otherUser.Id,
                    OtherUserName = $"{otherUser.FirstName} {otherUser.LastName}".Trim(),
                    OtherUserRole = otherUser.Role,
                    LastMessage = conversation.MessageText,
                    LastMessageAt = conversation.SentAt,
                    LastMessageIsRead = conversation.IsRead
                };
            })
            .ToList();

            return Ok(response);
        }
    }
}