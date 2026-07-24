using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("CompanyHolidays")]
    public class CompanyHoliday
    {
        [Key]
        public int HolidayId { get; set; }

        public string HolidayName { get; set; } = string.Empty;

        public DateTime HolidayDate { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}