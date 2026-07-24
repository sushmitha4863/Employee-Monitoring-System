using EmployeeMonitoringAPI.Models;

public class Team
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ManagerId { get; set; }

    public User? Manager { get; set; }

    public ICollection<TeamMember>? Members { get; set; }
}