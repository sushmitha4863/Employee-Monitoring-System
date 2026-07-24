using EmployeeMonitoringAPI.Models;

public class Department
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DepartmentCode { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<User>? Employees { get; set; }
}