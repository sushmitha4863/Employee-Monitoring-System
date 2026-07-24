using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models
{
    public class WorkTask
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? ManagerNotes { get; set; }
        public DateTime? StartDate { get; set; }

        public int AssignedToId { get; set; }
        public User? AssignedTo { get; set; }

        public int AssignedById { get; set; }
        public User? AssignedBy { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; }

        public string Priority { get; set; } = "Medium";
        public decimal EstimatedHours { get; set; }
        public string Status { get; set; } = "Pending";
        public int ProgressPercentage { get; set; }
        public string? EmployeeRemarks { get; set; }
        public string? ManagerFeedback { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
    }
}
