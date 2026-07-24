using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class RecruitmentCandidate
{
 [Key] public int CandidateId { get; set; }
 public int AssignedHrId { get; set; }
 public string FullName { get; set; } = string.Empty;
 public string Email { get; set; } = string.Empty;
 public string NormalizedEmail { get; set; } = string.Empty;
 public string Phone { get; set; } = string.Empty;
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 public ICollection<Applicant> JobApplications { get; set; } = new List<Applicant>();
}
