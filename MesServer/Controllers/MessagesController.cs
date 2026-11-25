using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MesServer.Models;
using MesServer.Models.DTOs;
using MesServer.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System;
using MesServer.Data;

namespace MesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;
        private readonly AppDbContext _context;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService messageService, AppDbContext context, ILogger<MessagesController> logger)
        {
            _messageService = messageService;
            _context = context;
            _logger = logger;
        }
        [HttpGet("GetChatMessages")]
        public async Task<IActionResult> GetChatMessages(
            [FromQuery] int chatId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int count = 100)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("userId: {userId}", userId);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }
                // Проверяем, что пользователь является участником чата
                var isParticipant = await _context.ChatParticipants
                    .AnyAsync(cp => cp.ChatID == chatId && cp.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogInformation("userId is not Participant Chat {chatId}", chatId);
                    return Forbid();
                }

                toDate ??= DateTime.UtcNow;
                fromDate ??= toDate.Value.AddDays(-30);

                var messages = await _context.Messages
                    .Include(m => m.Sender)
                    .Where(m => m.ChatID == chatId &&
                               m.SendDate > fromDate &&
                               m.SendDate <= toDate &&
                               !m.IsDeleted)
                    .OrderByDescending(m => m.SendDate)
                    .Take(count)
                    .Select(m => new
                    {
                        m.MessageID,
                        m.ChatID,
                        m.SenderID,
                        SenderName = m.Sender.UserName ?? "",
                        SenderFullName = $"{m.Sender.FirstName ?? ""} {m.Sender.LastName ?? ""}".Trim(),
                        m.MessageText,
                        m.SendDate,
                        m.IsRead,
                        m.IsEdited,
                        m.IsDeleted,
                        AttachmentURL = m.AttachmentURL ?? "",
                        IsMyMessage = m.SenderID == userId
                    })
                    .ToListAsync();

                // Обновляем статус прочтения
                _logger.LogInformation("Обновляем статус прочтения");
                await _context.Messages
                    .Where(m => m.ChatID == chatId &&
                               m.SenderID != userId &&
                               !m.IsRead &&
                               m.SendDate > fromDate &&
                               m.SendDate <= toDate)
                    .ForEachAsync(m => m.IsRead = true);

                //var chat = await _context.Chats
                //.Where(c => c.ChatID == dto.ChatID)




                //var contact = await _context.Contacts
                //    .FirstOrDefaultAsync(c => c.ChatId == chatId && c.UserId == userId);

                //if (contact != null && (contact.HaveNewMessage ?? false))
                //{
                //    _logger.LogInformation("contact != null");
                //    contact.HaveNewMessage = false;
                //    //await _context.SaveChangesAsync();
                //}
                await _context.SaveChangesAsync();

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for chat {ChatId}", chatId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("SendMessage")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
        {
            _logger.LogInformation("SendMessage");
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                // Проверяем существование чата
                _logger.LogInformation("Проверяем существование чата {ChatId}", dto.ChatID);
                //var chat = await _context.Chats
                //    .Where(c => c.ChatID == dto.ChatID)
                //    .Select(c => new {
                //        c.ChatID,
                //        ChatName = c.ChatName ?? string.Empty,
                //        c.ChatType,
                //        c.CreatedBy,
                //        c.CreationDate,
                //        c.LastMessageDate
                //    })
                //    .FirstOrDefaultAsync();
                var chat = await _context.Chats
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.ChatID == dto.ChatID);

                if (chat == null)
                {
                    _logger.LogWarning("Chat {ChatId} not found", dto.ChatID);
                    return NotFound("Chat not found");
                }

                // Проверяем, является ли пользователь участником чата
                _logger.LogInformation("Validating chat {ChatId} for user {UserId}", dto.ChatID, userId);
                var isParticipant = await _context.ChatParticipants
                    .AnyAsync(cp => cp.ChatID == dto.ChatID && cp.UserId == userId);

                if (!isParticipant)
                {
                    _logger.LogWarning("User {UserId} is not a participant of chat {ChatId}", userId, dto.ChatID);
                    return BadRequest("User is not a participant of this chat");
                }


                await _context.SaveChangesAsync();
                // Создаем сообщение
                _logger.LogInformation("Creating new message for chat {ChatId}", dto.ChatID);
                var TimeNow = DateTime.UtcNow;
                var message = new Message
                {
                    ChatID = dto.ChatID,
                    SenderID = userId,
                    MessageText = dto.MessageText,
                    SendDate = TimeNow,
                    IsRead = false,
                    IsEdited = false,
                    IsDeleted = false,
                    AttachmentURL = dto.AttachmentURL
                };


                //chat.LastMessageDate будет обновлен триггером БД tr_UpdateLastMessageDate_OnMessage

                //_logger.LogInformation("Message {MessageId} created successfully", message.MessageID);
                await using var transaction = await _context.Database.BeginTransactionAsync();
                // For private chats (type 1)
                if (chat.ChatType == 1)
                {
                    var ContactsInChat = await _context.Contacts
                        .Where(cp => cp.ChatId == dto.ChatID && cp.UserId != userId)
                        .ToListAsync();
                    foreach (var ContactInChat in ContactsInChat)
                    {

                        if (ContactInChat != null)
                        {
                            ContactInChat.HaveNewMessage = true;
                        }
                    }
                }
                try
                {
                    await _context.Database.ExecuteSqlRawAsync("DISABLE TRIGGER ALL ON Messages");
                    _context.Messages.Add(message);
                    await _context.SaveChangesAsync();

                    // Update last message date
                    var chatEntity = await _context.Chats.FindAsync(dto.ChatID);
                    chatEntity.LastMessageDate = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                finally
                {
                    await _context.Database.ExecuteSqlRawAsync("ENABLE TRIGGER ALL ON Messages");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chat {ChatId}", dto.ChatID);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}