using MesServer.Data;
using MesServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesServer.Services
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Chat>> GetUserChatsAsync(string userId)
        {
            return await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Include(cp => cp.Chat)
                .Select(cp => cp.Chat)
                .ToListAsync();
        }
    }
}