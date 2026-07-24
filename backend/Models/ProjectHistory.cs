using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("ProjectHistory")]
    public class ProjectHistory
    {
        [Key]
        public int HistoryId { get; set; }

        public int ProjectId { get; set; }

        public string OldStatus { get; set; } = string.Empty;

        public string NewStatus { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProjectId")]
        public Project? Project { get; set; }
    }
}