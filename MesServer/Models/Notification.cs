namespace MesServer.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public byte NotificationType { get; set; }
        public int SourceId { get; set; }
        public string NotificationText { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }

        public User User { get; set; }
    }
}
