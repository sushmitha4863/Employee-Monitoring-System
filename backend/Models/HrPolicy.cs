using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class HrPolicy { [Key] public int PolicyId { get; set; } public string Title { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public DateTime EffectiveDate { get; set; } public int? DocumentId { get; set; } public string Description { get; set; } = string.Empty; public bool IsActive { get; set; } = true; }
