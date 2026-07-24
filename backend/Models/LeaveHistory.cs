using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.Models;

public sealed class LeaveHistory
{
    [Key] public int LeaveHistoryId { get; set; }
    public int LeaveId { get; set; }
    public Leave? Leave { get; set; }
    [MaxLength(40)] public string FromStatus { get; set; } = string.Empty;
    [MaxLength(40)] public string ToStatus { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Remarks { get; set; }
    public int ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}