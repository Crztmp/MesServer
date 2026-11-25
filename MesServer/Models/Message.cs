using System.ComponentModel.DataAnnotations.Schema;

namespace MesServer.Models
{
    public class Message
    {
        public int MessageID { get; set; }
        //[ForeignKey("Chat")]
        public int? ChatID { get; set; }  
        public string SenderID { get; set; }
       
        public string MessageText { get; set; }  
        public DateTime SendDate { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public string ?AttachmentURL { get; set; }

        // Navigation properties
        [ForeignKey("ChatID")]
        public virtual Chat Chat { get; private set; } // setter приватный

        [ForeignKey("SenderID")]
        public virtual User Sender { get; private set; }
    }
}
