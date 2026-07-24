namespace EmployeeMonitoringAPI.DTOs
{
    public class CreateTaskDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ExpectedOutput { get; set; }
        public string? ManagerNotes { get; set; }
        public DateTime? StartDate { get; set; }
        public int AssignedToId { get; set; }
        public int? ProjectId { get; set; }
        public string Priority { get; set; } = "Medium";
        public decimal EstimatedHours { get; set; }
        public DateTime? DueDate { get; set; }
    }
}