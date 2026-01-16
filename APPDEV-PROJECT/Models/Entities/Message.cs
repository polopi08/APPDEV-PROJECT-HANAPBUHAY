namespace APPDEV_PROJECT.Models.Entities
{
    public class Message
    {
        public Guid MessageId { get; set; }

        // Foreign keys
        public Guid ConversationId { get; set; }
        public Guid SenderId { get; set; } // UserId

        // Message content
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; } = false;

        // Navigation properties
        public Conversation? Conversation { get; set; }
    }
}
