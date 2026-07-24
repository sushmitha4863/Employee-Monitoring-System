using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models
{
    public class CalendarEvent
    {
        [Key]
        public int EventId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string EventType { get; set; } = "Meeting";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Scheduled";
        public string? Location { get; set; }
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public string MeetingGroupId { get; set; } = Guid.NewGuid().ToString("N");
    }
}