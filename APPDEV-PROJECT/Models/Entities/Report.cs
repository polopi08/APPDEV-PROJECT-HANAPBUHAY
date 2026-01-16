namespace APPDEV_PROJECT.Models.Entities
{
    public class Report
    {
        public Guid ReportId { get; set; }

        // ===== Reporter Information =====
        public Guid ReporterId { get; set; }
        public User Reporter { get; set; }

        // ===== Reported Worker Information =====
        public Guid? ReportedWorkerId { get; set; }
        public Worker ReportedWorker { get; set; }

        // ===== Report Details =====
        public string Reason { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // ===== Status and Admin Notes =====
        public string Status { get; set; } = "Pending";
        public string AdminNotes { get; set; }

        // ===== Timestamps =====
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
