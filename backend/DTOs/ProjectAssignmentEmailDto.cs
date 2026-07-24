namespace EmployeeMonitoringAPI.DTOs
{
    public class ProjectAssignmentEmailDto
    {
        public int ProjectId { get; set; }
        public int EmployeeId { get; set; }
        public int? UserId { get; set; }
    }
}
