using EmployeeMonitoringAPI.Models;
public class ActivityLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User? User { get; set; }

    public string Action { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}