using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("Projects")]
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public string Priority { get; set; } = "Medium";

        public string Description { get; set; } = string.Empty;
        public string Client { get; set; } = string.Empty;
        public decimal Budget { get; set; }

        public string Status { get; set; } = "Queued";

        public int ProgressPercentage { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int? ManagerId { get; set; }

        public User? Manager { get; set; }

        public bool IsArchived { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
