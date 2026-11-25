using System;

namespace MesServer.Models
{
    public class Contact
    {
        public int ContactID { get; set; }
        public string UserId { get; set; }
        public int? ChatId { get; set; }
        public byte ChatType { get; set; }
        public string? ContactUserID { get; set; }
        public bool? Added { get; set; }
        public DateTime? AddedDate { get; set; } = DateTime.UtcNow;
        public bool? HaveNewMessage { get; set; }
        public bool? IsBlocked { get; set; }
        public bool? IsIgnored { get; set; }
        public bool? DoNotDisturb { get; set; }
        public DateTime? LastMessageLoadedDate { get; set; }
        public DateTime? LastMessageTime { get;  }
        public User? User { get; set; }
        public Chat? Chat { get; set; }
        public User? ContactUser { get; set; }
        public string? CustomDisplayName { get; set; }


    }
}
