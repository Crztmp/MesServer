using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MesServer.Data;
using MesServer.Models;
using MesServer.Models.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text.Json;
using System;
using Microsoft.Data.SqlClient;
using System.Data;

namespace MesServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ContactsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(AppDbContext context, ILogger<ContactsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        
        [HttpGet("getprived")]
        public async Task<IActionResult> GetContacts()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var contacts = await _context.Contacts
                    .Where(c => c.UserId == userId && c.ChatType == 1)
                    .Include(c => c.ContactUser)
                    .Select(c => new
                    {
                        ContactId = c.ContactID,
                        UserId = c.UserId,
                        ContactUserId = c.ContactUserID,
                        ContactName = c.CustomDisplayName ?? c.ContactUser.UserName,
                        ContactUser = new
                        {
                            c.ContactUser.Id,
                            c.ContactUser.UserName,
                            c.ContactUser.Email,
                            c.ContactUser.FirstName,
                            c.ContactUser.LastName
                        },
                        ChatId = c.ChatId,
                        //ChatType = 1,
                        Added =c.Added,
                        AddedDate = c.AddedDate,
                        DoNotDisturb = c.DoNotDisturb,
                        IsIgnored = c.IsIgnored,
                        IsBlocked = c.IsBlocked,
                        LastMessageTime = c.ChatId.HasValue ?
                            _context.Messages
                                .Where(m => m.ChatID == c.ChatId.Value)
                                .OrderByDescending(m => m.SendDate)
                                .Select(m => (DateTime?)m.SendDate)
                                .FirstOrDefault() :
                            null
                    })
                    .ToListAsync();

                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpGet("getchats")]
        public async Task<IActionResult> GetContactsChats()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                _logger.LogInformation("getchats, ChatType == 2");
                var contactsQuery = _context.Contacts
                    .Where(c => c.UserId == userId && c.ChatType == 2)
                    .Include(c => c.Chat) // Include Chat data
                    .Select(c => new
                    {
                        ContactId = c.ContactID,
                        UserId = c.UserId,
                        CustomDisplayName = c.CustomDisplayName,
                        ContactName = c.CustomDisplayName ?? c.Chat.ChatName,
                        ChatId = c.ChatId,
                        Chat=new  
                        { 
                            c.Chat.ChatID,
                            c.Chat.ChatName,
                            c.Chat.ChatDescription,
                            c.Chat.CreatedBy,
                            c.Chat.LastMessageDate,
                            c.Chat.ChatParticipants.Count

                        },
                        Added = c.Added,
                        AddedDate = c.AddedDate,
                        IsIgnored = c.IsIgnored,
                        IsBlocked = c.IsBlocked,
                        DoNotDisturb = c.DoNotDisturb,
                        LastMessageTime = c.Chat.LastMessageDate
                    });

                // Execute the query
                //var contacts = await contactsQuery.ToListAsync();
                // Execute the query first
                var contacts = await contactsQuery.ToListAsync();

                // Then apply the formatting in memory
                //var contacts = contactsList.Select(c => new
                //{
                //    c.ContactId,
                //    c.UserId,
                //    c.CustomDisplayName,
                //    c.ContactName,
                //    c.ChatId,
                //    c.Added,
                //    c.AddedDate,
                //    c.IsBlocked,
                //    LastMessageTime = c.LastMessageTime?.ToString("o")
                //}).ToList();
                
                _logger.LogInformation("getchats, Count {contacts.Count}", contacts.Count);
                foreach (var contact in contacts)
                {
                    //chat.ContactName ??= string.Empty;
                    _logger.LogInformation("id={contact.ContactId},name={contact.ContactName}, LMT={contact.LastMessageTime}", contact.ContactId,contact.ContactName, contact.LastMessageTime);
                }

                return Ok(contacts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contacts for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("AddPrivedContact")]
        public async Task<IActionResult> AddPrivedContact([FromBody] AddContactDto dto)
        {
            _logger.LogInformation("AddContact request received. UserId: {UserId}, ContactUserId: {ContactUserId}, CustomName: {CustomName}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                dto.ContactUserId,
                dto.CustomName);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("AddContact unauthorized - UserId is null or empty");
                return Unauthorized();
            }
            // Проверяем, существует ли пользователь
            var contactUser = await _context.Users.FindAsync(dto.ContactUserId);
            if (contactUser == null)
            {
                _logger.LogWarning("AddContact failed - Contact user not found. ContactUserId: {ContactUserId}", dto.ContactUserId);
                return NotFound("User not found");
            }
            // Проверяем, не добавляем ли себя
            if (userId == dto.ContactUserId)
            {
                _logger.LogWarning("AddContact failed - User tried to add themselves. UserId: {UserId}", userId);
                return BadRequest("Cannot add yourself as a contact");
            }
            try
            {
                // Проверяем, есть ли уже чат между пользователями
                var existingChat = await _context.ChatParticipants
                    .Where(cp => cp.UserId == userId)
                    .Join(
                        _context.ChatParticipants.Where(cp => cp.UserId == dto.ContactUserId),
                        cp1 => cp1.ChatID,
                        cp2 => cp2.ChatID,
                        (cp1, cp2) => cp1.Chat
                    )
                    .Where(c => c.ChatType == 1)
                    .FirstOrDefaultAsync();

                if (existingChat == null)
                {
                    _logger.LogInformation("AddContact  UserId: existingChat == null");
                    await _context.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_CreatePrivateChat @UserID, @ContactUserID, @CustomDisplayName",
                        new SqlParameter("@UserID", userId),
                        new SqlParameter("@ContactUserID", dto.ContactUserId),
                        new SqlParameter("@CustomDisplayName", string.IsNullOrEmpty(dto.CustomName) ? (object)DBNull.Value : dto.CustomName));


                    // Ищем только что созданный чат
                    existingChat = await _context.ChatParticipants
                        .Where(cp => cp.UserId == userId)
                        .Join(
                            _context.ChatParticipants.Where(cp => cp.UserId == dto.ContactUserId),
                            cp1 => cp1.ChatID,
                            cp2 => cp2.ChatID,
                            (cp1, cp2) => cp1.Chat
                        )
                        .Where(c => c.ChatType == 1)
                        .OrderByDescending(c => c.ChatID) // Берем самый новый
                        .FirstOrDefaultAsync();

                    if (existingChat == null)
                    {
                        _logger.LogWarning("AddContact failed - Cannot find created chat");
                        return BadRequest("Cannot find created chat");
                    }
                    else
                    {
                        return Ok(new
                        {
                            Message = "Contact added successfully",
                            ChatId = existingChat.ChatID
                        });
                    }
                }

                    _logger.LogInformation("AddContact  ChatID = {existingChat.ChatID}", existingChat.ChatID);

                _logger.LogInformation("AddContact  UserId: existingChat ID: {existingChat.ChatID}", existingChat.ChatID);

                // Добавляем участников
                var existingParticipant1 = await _context.ChatParticipants
                    .FirstOrDefaultAsync(cp => cp.ChatID == existingChat.ChatID && cp.UserId == userId);

                var existingParticipant2 = await _context.ChatParticipants
                    .FirstOrDefaultAsync(cp => cp.ChatID == existingChat.ChatID && cp.UserId == dto.ContactUserId);

                if (existingParticipant1 == null)
                {
                    _logger.LogInformation("Add new ChatParticipant: {userId}", userId);

                    _context.ChatParticipants.Add(new ChatParticipant
                    {
                        ChatID = existingChat.ChatID,
                        UserId = userId,
                        JoinDate = DateTime.UtcNow,
                        Role = 1
                    });
                }

                if (existingParticipant2 == null)
                {
                    _logger.LogInformation("Add new ChatParticipant: {dto.ContactUserId}", dto.ContactUserId);
                    _context.ChatParticipants.Add(new ChatParticipant
                    {
                        ChatID = existingChat.ChatID,
                        UserId = dto.ContactUserId,
                        JoinDate = DateTime.UtcNow,
                        Role = 1
                    });
                }


                // Проверяем, есть ли уже такой контакт
                var existingContactNotInContacts = await _context.Contacts
                .FirstOrDefaultAsync(c => c.UserId == userId
                && c.ContactUserID == dto.ContactUserId);

                if (existingContactNotInContacts != null)
                {

                    _logger.LogWarning("AddContact - Contact already exists, Renew. UserId: {UserId}, ContactUserId: {ContactUserId}",
                        userId, dto.ContactUserId);
                    existingContactNotInContacts.Added = true;
                    if (dto.CustomName != null)
                    {
                        existingContactNotInContacts.CustomDisplayName = dto.CustomName;

                    }
                    existingContactNotInContacts.AddedDate = DateTime.UtcNow;
                    existingContactNotInContacts.ChatId = existingChat.ChatID;
                    await _context.SaveChangesAsync();
                    //return Ok(new
                    //{
                    //    ContactId = existingContactNotInContacts.ContactID,
                    //    Message = "Contact added successfully"
                    //});
                }
                else
                {
                    // Создаем контакт
                    var contact = new Contact
                    {
                        UserId = userId,
                        ContactUserID = dto.ContactUserId,
                        CustomDisplayName = dto.CustomName,
                        Added = true,
                        AddedDate = DateTime.UtcNow,
                        ChatId = existingChat.ChatID
                    };
                    _context.Contacts.Add(contact);
                    _logger.LogInformation("AddContact successful. New contact created - ContactId: {ContactId}",
               contact.ContactID);
                    //existingContactNotInContacts = contact;
                }


                // Проверяем, есть ли уже такой обратный контакт
                var existingReverseContact = await _context.Contacts
                    .FirstOrDefaultAsync(c => c.UserId == dto.ContactUserId && c.ContactUserID == userId);

                if (existingReverseContact == null)
                {
                    // Создаем обратный контакт
                    var ReverceContact = new Contact
                    {
                        UserId = dto.ContactUserId,
                        ContactUserID = userId,
                        ChatId = existingChat.ChatID,
                        Added = false
                    };
                    _context.Contacts.Add(ReverceContact);
                    _logger.LogInformation("AddContact successful. New contact created - ContactId: {dto.ContactUserId}",
               dto.ContactUserId);
                }
                else 
                {
                    existingReverseContact.Added = false;
                    existingReverseContact.AddedDate = DateTime.UtcNow;
                    existingReverseContact.ChatId = existingChat.ChatID;
                    await _context.SaveChangesAsync();
                    _logger.LogWarning("AddContact - Contact already exists, Renew. UserId: {existingReverseContact.ContactUserID}",
                        existingReverseContact.ContactUserID);
                }

                await _context.SaveChangesAsync();
                

                return Ok(new
                {
                    //ContactId = contact.ContactID,
                    Message = "Contact added successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddContact failed unexpectedly. UserId: {UserId}, ContactUserId: {ContactUserId}",
                    userId, dto.ContactUserId);
                return StatusCode(500, "Internal server error");
            }
        }
        [HttpDelete("{contactId}")]
        public async Task<IActionResult> DeleteContact(int contactId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var contact = await _context.Contacts
                .FirstOrDefaultAsync(c => c.ContactID == contactId && c.UserId == userId);

            if (contact == null)
                return NotFound();

            // Удаляем чат, если это приватный чат
            var chat = await _context.Chats
                .FirstOrDefaultAsync(c => c.ChatID == contact.ChatId && c.ChatType == 1);

            if (chat != null)
            {
                // Удаляем участников чата
                var participants = await _context.ChatParticipants
                    .Where(cp => cp.ChatID == chat.ChatID)
                    .ToListAsync();

                _context.ChatParticipants.RemoveRange(participants);

                // Удаляем сообщения
                var messages = await _context.Messages
                    .Where(m => m.ChatID == chat.ChatID)
                    .ToListAsync();

                _context.Messages.RemoveRange(messages);

                // Удаляем сам чат
                _context.Chats.Remove(chat);
            }

            // Удаляем контакт
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Contact deleted successfully" });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchContacts([FromQuery] string term)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            try
            {
                var users = await _context.Users
                    .Where(u =>
                        u.UserName.Contains(term) ||
                        u.Email.Contains(term) ||
                        (u.FirstName + " " + u.LastName).Contains(term))
                    .Where(u => u.Id != userId) // Исключаем текущего пользователя
                    .Select(u => new ContactDetailsDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Username = u.UserName,
                        Email = u.Email,
                        FullName = $"{u.FirstName} {u.LastName}",
                        IsContact = _context.Contacts.Any(c =>
                            c.UserId == userId && c.ContactUserID == u.Id)
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching contacts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("UpdatePrivedContact")]
        public async Task<IActionResult> UpdatePrivedContact([FromBody] UpdateContactDto dto)
        {
            _logger.LogInformation("UpdateContact request received. UserId: {UserId}, ContactId: {ContactId}, CustomName: {CustomName}," +
                "IsIgnored: {dto.IsIgnored}, DoNotDisturb: {dto.DoNotDisturb}, IsBlocked: {dto.IsBlocked}",
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                dto.ContactId,dto.CustomName, dto.IsIgnored,dto.DoNotDisturb,dto.IsBlocked);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("UpdateContact unauthorized - UserId is null or empty");
                return Unauthorized();
            }
            try
            {
                var contact = await _context.Contacts.FindAsync(dto.ContactId);
                contact.CustomDisplayName = dto.CustomName;
                contact.IsBlocked = dto.IsBlocked;
                contact.IsIgnored = dto.IsIgnored;
                contact.DoNotDisturb = dto.DoNotDisturb;
                await _context.SaveChangesAsync();


                return Ok(new
                {
                    //ContactId = contact.ContactID,
                    Message = "Contact updateed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateContact failed unexpectedly. UserId: {UserId}, ContactId: {ContactId}",
                    userId, dto.ContactId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}