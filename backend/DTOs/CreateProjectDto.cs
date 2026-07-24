using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.DTOs;
public sealed class ProjectRequest
{
 [Required,MaxLength(150)] public string ProjectName { get; set; }=string.Empty;
 [Required,MaxLength(40)] public string ProjectCode { get; set; }=string.Empty;
 [MaxLength(1000)] public string Description { get; set; }=string.Empty;
 public int? DepartmentId { get; set; }
 public string Status { get; set; }="Queued";
 public string Priority { get; set; }="Medium";
 public DateTime StartDate { get; set; }
 public DateTime EndDate { get; set; }
 [Range(0,100)] public int ProgressPercentage { get; set; }
 public int? ManagerId { get; set; }
}