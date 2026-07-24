namespace EmployeeMonitoringAPI.DTOs;

public sealed class HrDashboardSummaryDto
{
    public int TotalEmployees { get; init; }
    public int ActiveEmployees { get; init; }
    public int Departments { get; init; }
    public int Managers { get; init; }
    public int AttendanceToday { get; init; }
    public int EmployeesPresent { get; init; }
    public int EmployeesAbsent { get; init; }
    public int EmployeesOnLeave { get; init; }
    public int PendingLeaveRequests { get; init; }
    public int ActiveProjects { get; init; }
    public int CompletedProjects { get; init; }
    public int OpenRecruitment { get; init; }
    public int Candidates { get; init; }
    public int InterviewsToday { get; init; }
    public int EmployeesOnboarding { get; init; }
    public int EmployeesOffboarding { get; init; }
    public int UpcomingBirthdays { get; init; }
    public int UpcomingAnniversaries { get; init; }
}