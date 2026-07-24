using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("Leaves")]
    public class Leave
    {
        [Key]
        public int LeaveId { get; set; }

        public int UserId { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public string LeaveType { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public string? AttachmentUrl { get; set; }
        public string? ClarificationRequest { get; set; }

        public bool IsHalfDay { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime AppliedAt { get; set; }

        // Approval Tracking

        public int? ApprovedById { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovalRemarks { get; set; }

        public DateTime? CancellationRequestedAt { get; set; }
        public string? CancellationReason { get; set; }
        public ICollection<LeaveHistory> History { get; set; } = new List<LeaveHistory>();

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ApprovedById")]
        public User? ApprovedBy { get; set; }
    }
}