// ===== NEW FILE ADDED FOR AUTHENTICATION SYSTEM =====
// This User entity stores account information for both Clients and Workers
// It handles registration and login functionality

namespace APPDEV_PROJECT.Models.Entities
{
    public class User
    {
        // Primary key - unique identifier for each user
        public Guid UserId { get; set; }

        // Email used for login - must be unique
        public string Email { get; set; } = string.Empty;

        // Hashed password for security
        public string PasswordHash { get; set; } = string.Empty;

        // User type: "Client" or "Worker" - determines which profile the user has
        public string UserType { get; set; } = string.Empty;

        // Account creation timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Last login timestamp
        public DateTime? LastLoginAt { get; set; }

        // Account status - whether the user can log in
        public bool IsActive { get; set; } = true;

        // ===== NAVIGATION PROPERTIES (for relationships) =====
        // One user can have one client profile (if user type is "Client")
        public Client? Client { get; set; }

        // One user can have one worker profile (if user type is "Worker")
        public Worker? Worker { get; set; }
    }
}
