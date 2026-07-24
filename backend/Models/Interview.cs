using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class Interview
{
 [Key] public int InterviewId { get; set; }
 public int AssignedHrId { get; set; }
 public int ApplicantId { get; set; }
 public int? InterviewerId { get; set; }
 public string Round { get; set; } = "Round 1";
 public DateTime ScheduledAt { get; set; }
 public DateTime EndTime { get; set; }
 public string Mode { get; set; } = "Online";
 public string Location { get; set; } = string.Empty;
 public string MeetingUrl { get; set; } = string.Empty;
 public string Feedback { get; set; } = string.Empty;
 public string Result { get; set; } = "Pending";
 public string Status { get; set; } = "Scheduled";
 public string Notes { get; set; } = string.Empty;
 public string HrRemarks { get; set; } = string.Empty;
 public Applicant? Applicant { get; set; }
}
