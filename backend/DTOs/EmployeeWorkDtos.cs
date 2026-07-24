namespace EmployeeMonitoringAPI.DTOs;

public sealed class WeeklyTimesheetLineDto
{
    public int ProjectId { get; set; }
    public int? TaskId { get; set; }
    public string? WorkDescription { get; set; }
    public string BillingType { get; set; } = "Billable";
    public decimal MondayHours { get; set; }
    public decimal TuesdayHours { get; set; }
    public decimal WednesdayHours { get; set; }
    public decimal ThursdayHours { get; set; }
    public decimal FridayHours { get; set; }
}
public sealed class SaveWeeklyTimesheetDto { public DateTime WeekStartDate { get; set; } public List<WeeklyTimesheetLineDto> Lines { get; set; } = []; }
public sealed class ReviewWeeklyTimesheetDto { public string Decision { get; set; } = ""; public string? Remarks { get; set; } }
public sealed class EmployeeTaskUpdateDto { public string Status { get; set; } = ""; public int ProgressPercentage { get; set; } public string? WorkNote { get; set; } }
