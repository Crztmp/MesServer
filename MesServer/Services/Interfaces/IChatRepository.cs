using MesServer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesServer.Services
{
    public interface IChatRepository
    {
        Task<List<Chat>> GetUserChatsAsync(string userId);
    }
}