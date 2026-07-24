using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public sealed class MfaChallenge
{
    [Key] public long Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    [MaxLength(64)] public string ChallengeId { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(128)] public string OtpHash { get; set; } = string.Empty;
    [MaxLength(40)] public string Purpose { get; set; } = "Login";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public int FailedAttempts { get; set; }
    public int ResendCount { get; set; }
    public DateTime LastSentAt { get; set; }
    public DateTime? InvalidatedAt { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }
    [MaxLength(500)] public string? UserAgent { get; set; }
}