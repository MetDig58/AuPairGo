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
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<LocationLog> LocationLogs { get; set; }
    }
}