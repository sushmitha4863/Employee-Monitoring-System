namespace EmployeeMonitoringAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Type { get; set; } = "General";
        public string? RelatedUrl { get; set; }
        public string? DeduplicationKey { get; set; }
    }
}