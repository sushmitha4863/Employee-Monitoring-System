using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class RecruitmentStatusHistory
{
 [Key] public int RecruitmentStatusHistoryId { get; set; }
 public int ApplicantId { get; set; }
 public int AssignedHrId { get; set; }
 public string FromStatus { get; set; } = string.Empty;
 public string ToStatus { get; set; } = string.Empty;
 public string Reason { get; set; } = string.Empty;
 public string Remarks { get; set; } = string.Empty;
 public int ChangedById { get; set; }
 public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
 public Applicant? Applicant { get; set; }
}
