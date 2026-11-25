using System.Collections.Generic;

namespace MesServer.Models
{
    public class Chat
    {
        public int ChatID { get; set; }
        public string ?ChatName { get; set; } = string.Empty;
        public string ?ChatDescription { get; set; }
        public byte ChatType { get; set; } = 1;
        public string? CreatedBy { get; set; }
        public DateTime? CreationDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageDate { get; set; }

        public ICollection<Message> Messages { get; set; }
        public ICollection<ChatParticipant> ChatParticipants { get; set; }
    }
}