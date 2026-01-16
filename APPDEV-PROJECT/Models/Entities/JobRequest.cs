namespace APPDEV_PROJECT.Models.Entities
{
    public class JobRequest
    {
        public Guid JobRequestId { get; set; }

        // Foreign keys
        public Guid ClientId { get; set; }
        public Guid WorkerId { get; set; }

        // Request details
        public string ServiceDetails { get; set; }
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected, Completed, Reviewed

        // Navigation properties
        public Client? Client { get; set; }
        public Worker? Worker { get; set; }
    }
}
