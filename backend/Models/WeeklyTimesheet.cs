using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models;

public sealed class WeeklyTimesheet
{
    [Key] public int Id { get; set; }
    public int EmployeeId { get; set; }
    public User? Employee { get; set; }
    public int ManagerId { get; set; }
    public User? Manager { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public string? ManagerRemarks { get; set; }
    public decimal TotalHours { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WeeklyTimesheetLine> Lines { get; set; } = new List<WeeklyTimesheetLine>();
}

public sealed class WeeklyTimesheetLine
{
    [Key] public int Id { get; set; }
    public int WeeklyTimesheetId { get; set; }
    public WeeklyTimesheet? WeeklyTimesheet { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? TaskId { get; set; }
    public WorkTask? Task { get; set; }
    [MaxLength(500)] public string? WorkDescription { get; set; }
    [MaxLength(20)] public string BillingType { get; set; } = "Billable";
    public decimal MondayHours { get; set; }
    public decimal TuesdayHours { get; set; }
    public decimal WednesdayHours { get; set; }
    public decimal ThursdayHours { get; set; }
    public decimal FridayHours { get; set; }
    public decimal TotalHours { get; set; }
}
