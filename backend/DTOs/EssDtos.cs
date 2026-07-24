namespace EmployeeMonitoringAPI.DTOs
{
    public class TimesheetEntryDto
    {
        public int? ProjectId { get; set; }
        public int? TaskId { get; set; }
        public DateTime WorkDate { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public bool IsBillable { get; set; }
        public string WorkDescription { get; set; } = string.Empty;
        public string? DailyNotes { get; set; }
    }

    public class TaskProgressDto
    {
        public string Status { get; set; } = "In Progress";
        public int ProgressPercentage { get; set; }
        public string? Remarks { get; set; }
    }

    public class EmployeePreferenceDto
    {
        public string Theme { get; set; } = "Light";
        public string DefaultLandingPage { get; set; } = "Dashboard";
        public bool EmailNotifications { get; set; } = true;
        public bool TaskReminders { get; set; } = true;
        public bool CalendarReminders { get; set; } = true;
        public bool TimesheetReminders { get; set; } = true;
        public string CalendarVisibility { get; set; } = "Visible to Manager/Admin";
        public string Language { get; set; } = "English";
        public string Timezone { get; set; } = "Asia/Kolkata";
    }

    public class AttendanceCorrectionDto
    {
        public int AttendanceId { get; set; }
        public DateTime RequestedCheckIn { get; set; }
        public DateTime RequestedCheckOut { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LimitedProfileDto
    {
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ThemePreference { get; set; }
        public string? NotificationPreference { get; set; }
    }
}
