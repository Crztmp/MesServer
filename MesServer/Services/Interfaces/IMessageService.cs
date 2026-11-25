using MesServer.Models;
using MesServer.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesServer.Services
{
    public interface IMessageService
    {
        Task<IEnumerable<Message>> GetMessagesAsync(int chatId);
        Task<Message> CreateMessageAsync(MessageCreateData data);
    }
}
