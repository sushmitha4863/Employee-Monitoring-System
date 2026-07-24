namespace EmployeeMonitoringAPI.DTOs
{
    public class UpdateTeamDto
    {
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int ManagerId { get; set; }
    }
}