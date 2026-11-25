namespace MesServer.Models.DTOs
{
    public class ContactDetailsDto
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsContact { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class ContactWithLastMessageDto
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string ContactName { get; set; }
        public string AvatarUrl { get; set; }
        public string LastMessage { get; set; }
        public DateTime? LastMessageDate { get; set; }
    }
}