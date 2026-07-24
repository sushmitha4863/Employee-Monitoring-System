using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.DTOs;
public sealed class CreateCalendarEventDto
{
 [Required,MaxLength(150)] public string Title { get; set; }=string.Empty;
 [MaxLength(1000)] public string? Description { get; set; }
 public string EventType { get; set; }="Meeting";
 public DateTime StartTime { get; set; }
 public DateTime EndTime { get; set; }
 [MaxLength(500)] public string? Location { get; set; }
 public int? ProjectId { get; set; }
 public List<int> UserIds { get; set; }=new();
 public string Status { get; set; }="Scheduled";
}