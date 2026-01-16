namespace APPDEV_PROJECT.Models.Entities
{
    public class Conversation
    {
        public Guid ConversationId { get; set; }

        // Foreign keys
        public Guid ClientId { get; set; }
        public Guid WorkerId { get; set; }
        public Guid JobRequestId { get; set; }

        // Conversation details
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public Client? Client { get; set; }
        public Worker? Worker { get; set; }
        public JobRequest? JobRequest { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
