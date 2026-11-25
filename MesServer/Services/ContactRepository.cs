using MesServer.Data;
using MesServer.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesServer.Services
{
    public class ContactRepository : IContactRepository
    {
        private readonly AppDbContext _context;

        public ContactRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Contact>> GetUserContactsAsync(string userId)
        {
            return await _context.Contacts
                .Where(c => c.UserId == userId)
                .Include(c => c.ContactUser)
                .ToListAsync();
        }
    }
}