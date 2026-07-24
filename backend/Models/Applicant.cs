using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class Applicant
{
 [Key] public int ApplicantId { get; set; }
 public int AssignedHrId { get; set; }
 public int? JobId { get; set; }
 public int CandidateId { get; set; }
 public string FullName { get; set; } = string.Empty;
 public string Email { get; set; } = string.Empty;
 public string NormalizedEmail { get; set; } = string.Empty;
 public string Phone { get; set; } = string.Empty;
 public string ResumeUrl { get; set; } = string.Empty;
 public string Qualification { get; set; } = string.Empty;
 public string Skills { get; set; } = string.Empty;
 public string Experience { get; set; } = string.Empty;
 public string ExperienceLevel { get; set; } = string.Empty;
 public string Remarks { get; set; } = string.Empty;
 public string RejectionReason { get; set; } = string.Empty;
 public string OfferLetterUrl { get; set; } = string.Empty;
 public string OfferStatus { get; set; } = string.Empty;
 public DateTime? OfferedAt { get; set; }
 public string Status { get; set; } = "Applied";
 public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
 public RecruitmentJob? Job { get; set; }
 public RecruitmentCandidate? Candidate { get; set; }
 public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
 public ICollection<RecruitmentStatusHistory> StatusHistory { get; set; } = new List<RecruitmentStatusHistory>();
}
