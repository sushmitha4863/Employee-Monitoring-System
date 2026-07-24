using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("Attendance")]
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int UserId { get; set; }

        public DateTime AttendanceDate { get; set; }

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public double TotalHours { get; set; }

        public DateTime? BreakStartTime { get; set; }

        public DateTime? BreakEndTime { get; set; }

        public int BreakMinutes { get; set; }

        public double EffectiveHours { get; set; }

        public int LateMinutes { get; set; }

        public double OvertimeHours { get; set; }

        public string AttendanceStatus { get; set; }
            = "Present";

        public string? WorkSummary { get; set; }

        public string? ManagerRemarks { get; set; }

        public string Status { get; set; } = "Present";

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
