namespace MesServer.Models.DTOs
{
    public class UpdateContactDto
    {
        public int ContactId { get; set; }
        public string CustomName { get; set; }
        public bool? IsBlocked { get; set; }
        public bool? IsIgnored { get; set; }
        public bool? DoNotDisturb { get; set; }
    }
}
