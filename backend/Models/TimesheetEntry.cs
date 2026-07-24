using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models
{
    public class TimesheetEntry
    {
        [Key]
        public int TimesheetEntryId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public int? TaskId { get; set; }
        public WorkTask? Task { get; set; }
        public DateTime WorkDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public double Hours { get; set; }
        public bool IsBillable { get; set; }
        public string WorkDescription { get; set; } = string.Empty;
        public string? DailyNotes { get; set; }
        public string Status { get; set; } = "Submitted";
        public string? ManagerComments { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedById { get; set; }
        public User? ApprovedBy { get; set; }
    }
}
