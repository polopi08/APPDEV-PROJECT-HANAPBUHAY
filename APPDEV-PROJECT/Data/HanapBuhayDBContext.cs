using Microsoft.EntityFrameworkCore; // we will use the functionality of the entity framework core to communicate with the database
using APPDEV_PROJECT.Models.Entities; // to access the Client model
namespace APPDEV_PROJECT.Data
{
    public class HanapBuhayDBContext: DbContext // dbcontext kasi we are making a database context class
    {
        // we are gpoing to make a constructor to pass the options to the base class

        public HanapBuhayDBContext(DbContextOptions<HanapBuhayDBContext> options) : base (options)
        {
            
        }
        public DbSet<Client> Clients { get; set; } // represent the clients table in the database

        // ===== NEW: Added Users DbSet for authentication =====
        // This table stores user accounts for login/registration
        public DbSet<User> Users { get; set; }

        // ===== NEW: Added Workers DbSet =====
        // This table stores worker profiles linked to user accounts
        public DbSet<Worker> Workers { get; set; }

        // ===== NEW: Configure relationships between User and Client/Worker =====
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure one-to-one relationship: One User can have one Client
            modelBuilder.Entity<User>()
                .HasOne(u => u.Client)
                .WithOne(c => c.User)
                .HasForeignKey<Client>(c => c.UserId);

            // ===== NEW: Configure one-to-one relationship: One User can have one Worker =====
            modelBuilder.Entity<User>()
                .HasOne(u => u.Worker)
                .WithOne(w => w.User)
                .HasForeignKey<Worker>(w => w.UserId);
        }
    }
}
