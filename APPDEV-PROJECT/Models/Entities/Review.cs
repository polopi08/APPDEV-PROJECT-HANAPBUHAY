namespace APPDEV_PROJECT.Models.Entities
{
    public class Review
    {
        public Guid ReviewId { get; set; }
        public Guid JobRequestId { get; set; }
        public Guid WorkerId { get; set; }
        public Guid ClientId { get; set; }
        
        public int Rating { get; set; } // 1-5 stars
        public string ReviewText { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Navigation properties
        public JobRequest? JobRequest { get; set; }
        public Worker? Worker { get; set; }
        public Client? Client { get; set; }
    }
}
