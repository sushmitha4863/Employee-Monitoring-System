using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
