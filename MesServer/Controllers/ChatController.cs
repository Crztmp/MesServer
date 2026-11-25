using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MesServer.Data;
using MesServer.Models;
using MesServer.Models.DTOs;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using Microsoft.Data.SqlClient;

namespace MesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    //[Authorize]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ChatController> _logger;

        public ChatController(AppDbContext context, ILogger<ChatController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("AddPublicChat")]
        public async Task<IActionResult> AddPublicChat([FromBody] AddChatDto dto)
        {
            _logger.LogInformation("AddContact request received. UserId: {UserId}, ContactUserId: {ContactUserId}, CustomName: {CustomName}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,dto.ChatId,dto.CustomName);

            // Проверка валидации модели
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddChat validation failed. Errors: {@Errors}", ModelState.Values.SelectMany(v => v.Errors));
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("AddContact unauthorized - UserId is null or empty");
                return Unauthorized();
            }
            try
            {
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(ch => ch.ChatID == dto.ChatId);
                if (existingChat == null) 
                {
                    _logger.LogWarning("Chat not Found, ChatID: {dto.ChatId}", dto.ChatId);
                    return StatusCode(500, "Internal server error");
                }

                    // Добавляем участника
                    var existingParticipant1 = await _context.ChatParticipants
                    .FirstOrDefaultAsync(cp => cp.ChatID == dto.ChatId && cp.UserId == userId);


                if (existingParticipant1 == null)
                {
                    _logger.LogInformation("Add new ChatParticipant: {userId}", userId);

                    _context.ChatParticipants.Add(new ChatParticipant
                    {
                        ChatID = dto.ChatId,
                        UserId = userId,
                        JoinDate = DateTime.UtcNow,
                        Role = 1
                    });
                }

                // Создаем контакт
                var existingcontact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.ChatId == dto.ChatId && c.UserId == userId);
                if (existingcontact == null)
                {
                    var contact = new Contact
                    {
                        UserId = userId,
                        ChatId = dto.ChatId,
                        ChatType= existingChat.ChatType,
                        CustomDisplayName = dto.CustomName,
                        Added = true,
                        AddedDate = DateTime.UtcNow,
                    };
                    _logger.LogInformation("Add new Chat: {dto.ChatId}", dto.ChatId);
                    _context.Contacts.Add(contact);
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("AddChat successful. New chat created - UserId: {dto.ChatId}", dto.ChatId);
                return Ok(new
                {
                    Message = "Chat added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddChat failed unexpectedly. ChatId: {dto.ChatId}", dto.ChatId);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpPost("createPublic")]
        public async Task<IActionResult> CreatePublicChat([FromBody] CreateChatDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("AddContact unauthorized - UserId is null or empty");
                return Unauthorized();
            }
            _logger.LogInformation("CreatePublicChat request: UserID = {userId}, ChatName={dto.ChatName}, ChatDescription={dto.ChatDescription}",
                userId, dto.ChatName, dto.ChatDescription);
            try
            {
                var existingChat = await _context.Chats
                    .FirstOrDefaultAsync(c => c.ChatName == dto.ChatName);
                if (existingChat != null)
                {
                    return BadRequest("Chat with this name already exists");
                } 
                else //{ }
                //if (existingChat == null)
                {
                    _logger.LogInformation("AddContact  UserId: existingChat == null");
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_CreatePublicChat @UserID, @ChatName,@ChatDescription",
                        new SqlParameter("@UserID", userId),
                        new SqlParameter("@ChatName", dto.ChatName),
                        new SqlParameter("@ChatDescription", dto.ChatDescription));

                    // Ищем только что созданный чат
                    existingChat = await _context.Chats
                        .FirstOrDefaultAsync(c => c.ChatName == dto.ChatName);


                    if (existingChat == null)
                    {
                        _logger.LogWarning("AddContact failed - Cannot find created chat");
                        return BadRequest("Cannot find created chat");
                    }
                    return Ok(new
                    {
                        Message = "Contact added successfully",
                        ChatId = existingChat.ChatID
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating chat");
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchChats([FromQuery] string term)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var chats = await _context.Chats
                    .Where(c =>c.ChatName.Contains(term))
//                    .Where(c => u.Id != userId) // Исключаем текущего пользователя
                    .Select(c => new Chat
                    {
                        ChatID = c.ChatID,
                        ChatName = c.ChatName,
                        ChatType = c.ChatType
                    })
                    .ToListAsync();

                return Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching contacts");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}