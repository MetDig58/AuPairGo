using Microsoft.EntityFrameworkCore;
using AuPairGo.API.Models;

namespace AuPairGo.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) //
            : base(options)                                                         // Dont really understand
        {                                                                           //
        }                                                                           //

        public DbSet<User> Users { get; set; }
        public DbSet<AuPairProfile> AuPairProfiles { get; set; }
        public DbSet<ParentProfile> ParentProfiles { get; set; }
        public DbSet<ChildProfile> ChildProfiles { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Status> Statuses { get; set; }
        public DbSet<ParentChildLink> ParentChildLinks { get; set; }
        public DbSet<LocationLog> LocationLogs { get; set; }
        public DbSet<DutyTask> DutyTasks { get; set; }
        public DbSet<NotificationLog> NotificationLogs { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Global Query Filter: Automatically exclude soft-deleted children from ALL queries
            modelBuilder.Entity<ChildProfile>()
                .HasQueryFilter(c => !c.IsDeleted);

            modelBuilder.Entity<Address>()
                .HasOne(address => address.ParentProfile)
                .WithMany()
                .HasForeignKey(address => address.ParentProfileId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(message => message.SenderUser)
                .WithMany()
                .HasForeignKey(message => message.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(message => message.RecipientUser)
                .WithMany()
                .HasForeignKey(message => message.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}