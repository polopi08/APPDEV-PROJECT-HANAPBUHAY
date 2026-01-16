namespace APPDEV_PROJECT.Models.Entities
{
    public class Notification
    {
        public Guid NotificationId { get; set; }

        // Foreign keys
        public Guid RecipientId { get; set; } // UserId of the person receiving the notification
        public Guid? JobRequestId { get; set; } // Optional link to job request
        public Guid? SenderId { get; set; } // UserId of the person sending the notification (optional)

        // Notification details
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "booking", "message", "review", "system", "booking-cancelled"
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public JobRequest? JobRequest { get; set; }
    }
}
