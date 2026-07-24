using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models
{
    public class DocumentFile
    {
        [Key]
        public int DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string Category { get; set; } = "General";
        public string FilePath { get; set; } = string.Empty;
        public int UploadedById { get; set; }
        public int? ProjectId { get; set; }
        public Project? Project { get; set; }
        public User? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
