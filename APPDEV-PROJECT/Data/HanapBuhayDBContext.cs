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

        // ===== NEW: Added JobRequest DbSet =====
        // This table stores job booking requests from clients to workers
        public DbSet<JobRequest> JobRequests { get; set; }

        // ===== NEW: Added Notifications DbSet =====
        // This table stores notifications for users
        public DbSet<Notification> Notifications { get; set; }

        // ===== NEW: Added Conversations DbSet =====
        // This table stores conversations between clients and workers
        public DbSet<Conversation> Conversations { get; set; }

        // ===== NEW: Added Messages DbSet =====
        // This table stores messages in conversations
        public DbSet<Message> Messages { get; set; }

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

            // ===== NEW: Configure relationships for JobRequest =====
            modelBuilder.Entity<JobRequest>()
                .HasOne(j => j.Client)
                .WithMany()
                .HasForeignKey(j => j.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<JobRequest>()
                .HasOne(j => j.Worker)
                .WithMany()
                .HasForeignKey(j => j.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== NEW: Configure relationships for Notification =====
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.JobRequest)
                .WithMany()
                .HasForeignKey(n => n.JobRequestId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== NEW: Configure relationships for Conversation =====
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Client)
                .WithMany()
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Worker)
                .WithMany()
                .HasForeignKey(c => c.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.JobRequest)
                .WithMany()
                .HasForeignKey(c => c.JobRequestId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== NEW: Configure relationships for Message =====
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
