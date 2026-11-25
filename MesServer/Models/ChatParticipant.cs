namespace MesServer.Models
{
    public class ChatParticipant
    {
        public int ParticipantID { get; set; }
        public int ChatID { get; set; }
        public string UserId { get; set; }
        public DateTime JoinDate { get; set; } = DateTime.UtcNow;
        public byte Role { get; set; } = 1;

        public Chat Chat { get; set; }
        public User User { get; set; }
    }
}
