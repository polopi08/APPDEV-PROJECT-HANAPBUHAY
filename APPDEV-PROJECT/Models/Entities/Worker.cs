namespace APPDEV_PROJECT.Models.Entities
{
    public class Worker
    {
        public Guid WorkerId { get; set; }

        // ===== NEW: Foreign key to link Worker to User account =====
        // This connects the profile to the authentication user
        public Guid UserId { get; set; }

        public string LName { get; set; }

        public string FName { get; set; }

        public string Mname { get; set; }

        public string Email { get; set; }

        public DateTime DateOfBirth { get; set; }

        public string Sex { get; set; }

        public string PhoneNumber { get; set; }

        public string Address { get; set; }

        // ===== NEW: Location coordinates for geolocation =====
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // ===== Work Background Information =====
        public string Skill { get; set; }

        public int YearsOfExperience { get; set; }

        public string Accomplishments { get; set; }

        public string FullName => $"{FName} {LName}";

        // ===== NEW: Job completion and rating statistics =====
        public int CompletedJobs { get; set; } = 0;
        public double AverageRating { get; set; } = 0.0;

        // ===== NEW: Navigation property to access the User account =====
        // This allows you to access user info from worker (e.g., worker.User.Email)
        public User? User { get; set; }
    }
}
