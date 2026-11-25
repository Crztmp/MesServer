using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace MesServer.Models
{
    public class User : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastOnline { get; set; }
        public bool IsActiveDirectory { get; set; }
        public string? DomainName { get; set; }
        public DateTime? LastADSyncDate { get; set; }

    }
}