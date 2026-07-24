using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("ProjectAssignments")]
    public class ProjectAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        public int ProjectId { get; set; }

        public int UserId { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}