namespace EmployeeMonitoringAPI.DTOs
{
    public class CreateEmployeeDto
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Employee";

        public string? EmployeeCode { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Designation { get; set; }

        public DateTime? DateOfJoining { get; set; }

        public int? DepartmentId { get; set; }

        public int? ManagerId { get; set; }
    }
}