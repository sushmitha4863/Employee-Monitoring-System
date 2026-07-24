using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models
{
    public class EmployeePreference
    {
        [Key]
        public int EmployeePreferenceId { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string Theme { get; set; } = "Light";
        public string DefaultLandingPage { get; set; } = "Dashboard";
        public bool EmailNotifications { get; set; } = true;
        public bool TaskReminders { get; set; } = true;
        public bool CalendarReminders { get; set; } = true;
        public bool TimesheetReminders { get; set; } = true;
        public string CalendarVisibility { get; set; } = "Visible to Manager/Admin";
        public string Language { get; set; } = "English";
        public string Timezone { get; set; } = "Asia/Kolkata";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
