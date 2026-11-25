using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MesServer.Models;
//using EntityFrameworkCore.Triggers;

namespace MesServer.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ChatUser>()
                .HasKey(cu => new { cu.UserId, cu.ChatId });
            builder.Entity<User>().ToTable("AspNetUsers");

            builder.Entity<ChatParticipant>()
                .HasKey(cp => cp.ParticipantID);

            builder.Entity<Contact>()
                .HasKey(c => c.ContactID);

            builder.Entity<Contact>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Contact>()
                .HasOne(c => c.ContactUser)
                .WithMany()
                .HasForeignKey(c => c.ContactUserID)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>(entity =>
            {
                entity.HasKey(m => m.MessageID);
                entity.Property(m => m.MessageID).ValueGeneratedOnAdd();
                entity.HasIndex(m => m.ChatID);
                entity.HasIndex(m => m.SenderID);
            });
        }
    }
}
