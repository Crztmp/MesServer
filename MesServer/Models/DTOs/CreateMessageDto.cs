namespace MesServer.Models.DTOs
{
    public class CreateMessageDto
    {
        public int ChatID { get; set; }
        public string MessageText { get; set; }
        public string? AttachmentURL { get; set; } = null;
        public bool IsRead { get; set; } = false;
        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime SendDate { get; set; } = DateTime.UtcNow;
    }
}