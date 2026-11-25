namespace MesServer.Models.DTOs
{
    public class ChatMessageDto
    {
        public int MessageID { get; set; }
        public int ChatID { get; set; }
        public string SenderID { get; set; }
        public string SenderName { get; set; }
        public string SenderFullName { get; set; }
        public string MessageText { get; set; }
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public string AttachmentURL { get; set; }
        public bool IsMyMessage { get; set; }
    }
}
