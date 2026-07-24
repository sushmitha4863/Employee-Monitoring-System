using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public sealed class AuditLog
{
    [Key] public long Id { get; set; }
    public int? ActorUserId { get; set; }
    [MaxLength(120)] public string Action { get; set; } = string.Empty;
    [MaxLength(80)] public string EntityType { get; set; } = string.Empty;
    [MaxLength(80)] public string? EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [MaxLength(30)] public string Result { get; set; } = "Success";
    public string? SafeMetadata { get; set; }
    [MaxLength(100)] public string? CorrelationId { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
}