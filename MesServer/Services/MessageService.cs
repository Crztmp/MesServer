using Microsoft.EntityFrameworkCore;
using MesServer.Data;
using MesServer.Models;
using MesServer.Models.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MesServer.Services
{
    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;

        public MessageService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Message>> GetMessagesAsync(int chatId)
        {
            return await _context.Messages
                .Include(m => m.Sender) // Включаем информацию об отправителе
                .Where(m => m.ChatID == chatId)
                .OrderBy(m => m.SendDate)
                .ToListAsync();
        }

        public async Task<Message> CreateMessageAsync(MessageCreateData data)
        {
            var message = new Message
            {
                MessageText = data.Text,
                ChatID = data.ChatId,
                SenderID = data.UserId,
                SendDate = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return message;
        }
    }
}