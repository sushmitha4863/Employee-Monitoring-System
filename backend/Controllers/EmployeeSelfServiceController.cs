using ClosedXML.Excel;
using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Employee,Manager,HR")]
    public class EmployeeSelfServiceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EmployeeSelfServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = CurrentUserId();
            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var user = await _context.Users.Include(x => x.Department).Include(x => x.Manager).FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null) return Unauthorized();

            var attendance = await _context.Attendances.Where(x => x.UserId == userId && x.AttendanceDate >= monthStart && x.AttendanceDate <= monthEnd).OrderByDescending(x => x.AttendanceDate).ToListAsync();
            var todayAttendance = attendance.FirstOrDefault(x => x.AttendanceDate.Date == today);
            var leaves = await _context.Leaves.Where(x => x.UserId == userId).OrderByDescending(x => x.AppliedAt).ToListAsync();
            var leaveBalance = await GetOrCreateLeaveBalance(userId);
            var projects = await _context.ProjectAssignments.Include(x => x.Project).ThenInclude(x => x!.Manager).Where(x => x.UserId == userId).Select(x => x.Project).Where(x => x != null).ToListAsync();
            var tasks = await _context.WorkTasks.Include(x => x.Project).Where(x => x.AssignedToId == userId).OrderBy(x => x.DueDate).ToListAsync();
            var notifications = await _context.Notifications.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync();
            var activities = await _context.ActivityLogs.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync();
            var holidays = await _context.CompanyHolidays.Where(x => x.HolidayDate >= today).OrderBy(x => x.HolidayDate).Take(6).ToListAsync();
            var birthdays = await _context.Users.Where(x => x.Role != "Admin" && x.DateOfBirth.HasValue && x.DateOfBirth.Value.Month == today.Month && x.DateOfBirth.Value.Day >= today.Day).OrderBy(x => x.DateOfBirth!.Value.Day).Take(6).Select(x => new { x.UserId, x.FullName, x.DateOfBirth }).ToListAsync();
            var calendar = await BuildCalendarPreview(userId, today, today.AddDays(14));

            var workDays = Enumerable.Range(0, (monthEnd - monthStart).Days + 1).Select(offset => monthStart.AddDays(offset)).Count(x => x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday);
            var presentDays = attendance.Count(x => x.AttendanceStatus == "Present" || x.Status == "Present");
            var completedTasks = tasks.Count(x => x.Status == "Completed");
            var taskCompletion = tasks.Count == 0 ? 0 : Math.Round((double)completedTasks / tasks.Count * 100);
            var projectProgress = projects.Count == 0 ? 0 : Math.Round(projects.Average(x => x!.ProgressPercentage));
            var lateCount = attendance.Count(x => x.LateMinutes > 0);
            var attendancePercentage = workDays == 0 ? 0 : Math.Min(100, Math.Round((double)presentDays / workDays * 100));
            var punctuality = attendance.Count == 0 ? 0 : Math.Round((double)(attendance.Count - lateCount) / attendance.Count * 100);
            var performanceScore = Math.Max(0, Math.Round((attendancePercentage * .35) + (taskCompletion * .35) + (projectProgress * .20) + (punctuality * .10)));

            return Ok(new
            {
                employee = new { user.UserId, user.FullName, user.Email, user.EmployeeCode, user.Designation, Department = user.Department?.Name, Manager = user.Manager?.FullName, user.ProfileImage },
                todayAttendance = todayAttendance == null ? null : new { todayAttendance.AttendanceId, todayAttendance.AttendanceDate, todayAttendance.CheckInTime, todayAttendance.CheckOutTime, todayAttendance.TotalHours, todayAttendance.BreakMinutes, todayAttendance.EffectiveHours, todayAttendance.LateMinutes, todayAttendance.OvertimeHours, todayAttendance.AttendanceStatus, todayAttendance.Status, todayAttendance.WorkSummary },
                weeklyHours = Math.Round(attendance.Where(x => x.AttendanceDate >= weekStart).Sum(x => x.EffectiveHours > 0 ? x.EffectiveHours : x.TotalHours), 2),
                monthlyHours = Math.Round(attendance.Sum(x => x.EffectiveHours > 0 ? x.EffectiveHours : x.TotalHours), 2),
                attendancePercentage,
                leaveBalance = new { total = leaveBalance.TotalLeaves, used = leaveBalance.UsedLeaves, remaining = leaveBalance.RemainingLeaves },
                pendingLeaves = leaves.Count(x => x.Status == "Pending"),
                upcomingHolidays = holidays.Select(x => new { x.HolidayId, Name = x.HolidayName, x.HolidayDate }),
                upcomingBirthdays = birthdays,
                assignedProjects = projects.Select(x => new { x!.ProjectId, x.ProjectName, x.Description, ManagerName = x.Manager != null ? x.Manager.FullName : null, x.ProgressPercentage, x.Status, x.EndDate }).ToList(),
                assignedTasks = tasks.Select(x => new { x.TaskId, x.Title, x.Description, x.Priority, x.Status, x.ProgressPercentage, x.DueDate, ProjectName = x.Project != null ? x.Project.ProjectName : null }).ToList(),
                overdueTasks = tasks.Count(x => x.DueDate.HasValue && x.DueDate.Value.Date < today && x.Status != "Completed"),
                projectProgress,
                taskCompletionPercentage = taskCompletion,
                upcomingDeadlines = tasks.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date >= today).Take(6).Select(x => new { x.TaskId, x.Title, x.DueDate, Type = "Task" }).ToList(),
                recentNotifications = notifications.Select(x => new { x.Id, x.Title, x.Message, x.IsRead, x.CreatedAt }).ToList(),
                announcements = notifications.Where(x => x.Title.Contains("Announcement")).Take(5).Select(x => new { x.Id, x.Title, x.Message, x.IsRead, x.CreatedAt }).ToList(),
                recentActivities = activities.Select(x => new { x.Id, x.Action, x.CreatedAt }),
                calendarPreview = calendar,
                performanceSummary = new { attendanceScore = attendancePercentage, taskCompletion, projectCompletion = projectProgress, punctuality, lateMarks = lateCount, leaveImpact = leaves.Count(x => x.Status == "Approved"), productivity = performanceScore, performanceScore }
            });
        }

        [HttpGet("preferences")]
        public async Task<IActionResult> GetPreferences()
        {
            var pref = await GetOrCreatePreference(CurrentUserId());
            return Ok(pref);
        }

        [HttpPut("preferences")]
        public async Task<IActionResult> SavePreferences(EmployeePreferenceDto dto)
        {
            var pref = await GetOrCreatePreference(CurrentUserId());
            pref.Theme = string.IsNullOrWhiteSpace(dto.Theme) ? "Light" : dto.Theme;
            pref.DefaultLandingPage = string.IsNullOrWhiteSpace(dto.DefaultLandingPage) ? "Dashboard" : dto.DefaultLandingPage;
            pref.EmailNotifications = dto.EmailNotifications;
            pref.TaskReminders = dto.TaskReminders;
            pref.CalendarReminders = dto.CalendarReminders;
            pref.TimesheetReminders = dto.TimesheetReminders;
            pref.CalendarVisibility = string.IsNullOrWhiteSpace(dto.CalendarVisibility) ? "Visible to Manager/Admin" : dto.CalendarVisibility;
            pref.Language = string.IsNullOrWhiteSpace(dto.Language) ? "English" : dto.Language;
            pref.Timezone = string.IsNullOrWhiteSpace(dto.Timezone) ? "Asia/Kolkata" : dto.Timezone;
            pref.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Preferences saved" });
        }

        [HttpGet("announcements")]
        public async Task<IActionResult> Announcements()
        {
            var userId = CurrentUserId();
            var items = await _context.Notifications
                .Where(x => x.UserId == userId && (x.Title.Contains("Announcement") || x.Title.Contains("HR Update") || x.Title.Contains("Company")))
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new { x.Id, x.Title, Description = x.Message, Priority = "Normal", CreatedBy = "Organization", CreatedDate = x.CreatedAt, ExpiryDate = (DateTime?)null, x.IsRead })
                .ToListAsync();
            return Ok(items);
        }

        [HttpGet("activity")]
        public async Task<IActionResult> Activity()
        {
            var userId = CurrentUserId();
            var items = await _context.ActivityLogs.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
            return Ok(items);
        }

        [HttpGet("reports/{type}/export")]
        public async Task<IActionResult> ExportPersonalReport(string type, string format = "excel")
        {
            var rows = await BuildPersonalReportRows(CurrentUserId(), type);
            if (!rows.Any()) rows.Add(new Dictionary<string, object?> { ["Message"] = "No records found" });
            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase))
            {
                var csv = BuildCsv(rows);
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", $"my-{type}-report.csv");
            }
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                return File(Encoding.UTF8.GetBytes(BuildTextReport(rows, type)), "application/pdf", $"my-{type}-report.pdf");
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(type.Length > 31 ? type[..31] : type);
            var headers = rows.First().Keys.ToList();
            for (var i = 0; i < headers.Count; i++) worksheet.Cell(1, i + 1).Value = headers[i];
            for (var r = 0; r < rows.Count; r++) for (var c = 0; c < headers.Count; c++) worksheet.Cell(r + 2, c + 1).Value = rows[r].GetValueOrDefault(headers[c])?.ToString();
            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"my-{type}-report.xlsx");
        }

        private async Task<List<object>> BuildCalendarPreview(int userId, DateTime from, DateTime to)
        {
            var result = new List<object>();
            var holidays = await _context.CompanyHolidays.Where(x => x.HolidayDate >= from && x.HolidayDate <= to).ToListAsync();
            result.AddRange(holidays.Select(x => new { date = x.HolidayDate, title = x.HolidayName, type = "Holiday" }));
            var leaves = await _context.Leaves.Where(x => x.UserId == userId && x.Status == "Approved" && x.FromDate <= to && x.ToDate >= from).ToListAsync();
            result.AddRange(leaves.Select(x => new { date = x.FromDate, title = x.LeaveType, type = "Leave" }));
            var tasks = await _context.WorkTasks.Where(x => x.AssignedToId == userId && x.DueDate >= from && x.DueDate <= to).ToListAsync();
            result.AddRange(tasks.Select(x => new { date = x.DueDate, title = x.Title, type = "Task Deadline" }));
            var events = await _context.CalendarEvents.Where(x => x.UserId == userId && x.StartTime >= from && x.StartTime <= to).ToListAsync();
            result.AddRange(events.Select(x => new { date = x.StartTime, title = x.Title, type = x.EventType }));
            return result;
        }

        private async Task<List<Dictionary<string, object?>>> BuildPersonalReportRows(int userId, string type)
        {
            switch (type.ToLowerInvariant())
            {
                case "attendance":
                    var attendance = await _context.Attendances.Where(x => x.UserId == userId).OrderByDescending(x => x.AttendanceDate).ToListAsync();
                    return attendance.Select(x => new Dictionary<string, object?> { ["Date"] = x.AttendanceDate.ToString("yyyy-MM-dd"), ["CheckIn"] = x.CheckInTime?.ToString("HH:mm"), ["CheckOut"] = x.CheckOutTime?.ToString("HH:mm"), ["Hours"] = x.TotalHours, ["BreakMinutes"] = x.BreakMinutes, ["EffectiveHours"] = x.EffectiveHours, ["Status"] = x.AttendanceStatus }).ToList();
                case "leave":
                    var leaves = await _context.Leaves.Where(x => x.UserId == userId).OrderByDescending(x => x.AppliedAt).ToListAsync();
                    return leaves.Select(x => new Dictionary<string, object?> { ["Type"] = x.LeaveType, ["From"] = x.FromDate.ToString("yyyy-MM-dd"), ["To"] = x.ToDate.ToString("yyyy-MM-dd"), ["Reason"] = x.Reason, ["Status"] = x.Status }).ToList();
                case "projects":
                case "project":
                    var projects = await _context.ProjectAssignments.Include(x => x.Project).Where(x => x.UserId == userId).ToListAsync();
                    return projects.Select(x => new Dictionary<string, object?> { ["Project"] = x.Project?.ProjectName, ["Status"] = x.Project?.Status, ["Progress"] = x.Project?.ProgressPercentage, ["Deadline"] = x.Project?.EndDate.ToString("yyyy-MM-dd") }).ToList();
                case "timesheets":
                case "timesheet":
                    var timesheets = await _context.TimesheetEntries.Include(x => x.Project).Include(x => x.Task).Where(x => x.UserId == userId).OrderByDescending(x => x.WorkDate).ToListAsync();
                    return timesheets.Select(x => new Dictionary<string, object?> { ["Date"] = x.WorkDate.ToString("yyyy-MM-dd"), ["Project"] = x.Project?.ProjectName, ["Task"] = x.Task?.Title, ["Hours"] = x.Hours, ["Billable"] = x.IsBillable, ["Status"] = x.Status }).ToList();
                case "performance":
                    var dashboard = await BuildPersonalReportRows(userId, "attendance");
                    var tasks = await _context.WorkTasks.Where(x => x.AssignedToId == userId).ToListAsync();
                    return new List<Dictionary<string, object?>> { new() { ["AttendanceRecords"] = dashboard.Count, ["Tasks"] = tasks.Count, ["CompletedTasks"] = tasks.Count(x => x.Status == "Completed"), ["TaskCompletion"] = tasks.Count == 0 ? 0 : Math.Round((double)tasks.Count(x => x.Status == "Completed") / tasks.Count * 100) } };
                default:
                    return new List<Dictionary<string, object?>>();
            }
        }

        private async Task<LeaveBalance> GetOrCreateLeaveBalance(int userId)
        {
            var balance = await _context.LeaveBalances.FirstOrDefaultAsync(x => x.UserId == userId);
            if (balance != null) return balance;
            balance = new LeaveBalance { UserId = userId, TotalLeaves = 12, UsedLeaves = 0 };
            _context.LeaveBalances.Add(balance);
            await _context.SaveChangesAsync();
            return balance;
        }

        private async Task<EmployeePreference> GetOrCreatePreference(int userId)
        {
            var pref = await _context.EmployeePreferences.FirstOrDefaultAsync(x => x.UserId == userId);
            if (pref != null) return pref;
            pref = new EmployeePreference { UserId = userId };
            _context.EmployeePreferences.Add(pref);
            await _context.SaveChangesAsync();
            return pref;
        }

        private static string BuildCsv(List<Dictionary<string, object?>> rows)
        {
            var headers = rows.First().Keys.ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));
            foreach (var row in rows) sb.AppendLine(string.Join(",", headers.Select(h => $"\"{row.GetValueOrDefault(h)?.ToString()?.Replace("\"", "\"\"")}\"")));
            return sb.ToString();
        }

        private static string BuildTextReport(List<Dictionary<string, object?>> rows, string type)
        {
            return $"{type} report{Environment.NewLine}" + string.Join(Environment.NewLine, rows.Select(row => string.Join(" | ", row.Select(x => $"{x.Key}: {x.Value}"))));
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}


