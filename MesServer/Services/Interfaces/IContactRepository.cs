using MesServer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesServer.Services
{
    public interface IContactRepository
    {
        Task<List<Contact>> GetUserContactsAsync(string userId);
    }
}