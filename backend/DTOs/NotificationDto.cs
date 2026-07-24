namespace EmployeeMonitoringAPI.DTOs
{
    public class NotificationDto
    {
        public int? UserId { get; set; }
        public string? Role { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}