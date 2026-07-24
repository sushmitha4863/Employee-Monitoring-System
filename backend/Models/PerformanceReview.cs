using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class PerformanceReview
{
    [Key] public int PerformanceReviewId { get; set; }
    public int EmployeeId { get; set; }
    public int? ReviewerId { get; set; }
    public int Year { get; set; }
    [MaxLength(2)] public string Quarter { get; set; } = "Q1";
    public decimal WorkQuality { get; set; }
    public decimal Timeliness { get; set; }
    public decimal Collaboration { get; set; }
    public decimal Communication { get; set; }
    public decimal TechnicalContribution { get; set; }
    public decimal ProjectContribution { get; set; }
    public decimal Rating { get; set; }
    public string Goals { get; set; } = string.Empty;
    public string Kpis { get; set; } = string.Empty;
    public decimal TrainingScore { get; set; }
    public decimal BehaviorScore { get; set; }
    public string PromotionRecommendation { get; set; } = "Not Recommended";
    [MaxLength(100)] public string RecommendedDesignation { get; set; } = string.Empty;
    [MaxLength(2000)] public string Strengths { get; set; } = string.Empty;
    [MaxLength(2000)] public string ImprovementAreas { get; set; } = string.Empty;
    public string ManagerRemarks { get; set; } = string.Empty;
    public bool IsReleased { get; set; }
    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;
}