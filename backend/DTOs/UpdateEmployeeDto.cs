namespace EmployeeMonitoringAPI.DTOs
{
    public class UpdateEmployeeDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string? EmployeeCode { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Designation { get; set; }

        public DateTime? DateOfJoining { get; set; }

        public bool IsActive { get; set; }

        public int? DepartmentId { get; set; }

        public int? ManagerId { get; set; }
    }
}