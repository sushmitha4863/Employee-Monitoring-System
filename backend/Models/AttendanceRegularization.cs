using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    public class AttendanceRegularization
    {
        [Key]
        public int RequestId { get; set; }

        public int UserId { get; set; }

        public int AttendanceId { get; set; }

        public DateTime RequestedCheckIn { get; set; }

        public DateTime RequestedCheckOut { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; }
            = "Pending";

        public DateTime RequestedAt { get; set; }
            = DateTime.UtcNow;

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? ManagerRemarks { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [ForeignKey(nameof(AttendanceId))]
        public Attendance? Attendance { get; set; }

        [ForeignKey(nameof(ApprovedById))]
        public User? ApprovedBy { get; set; }
    }
}