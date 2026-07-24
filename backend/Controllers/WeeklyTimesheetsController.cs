using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers;

[ApiController, Route("api/employee/timesheets"), Authorize]
public sealed class WeeklyTimesheetsController(ApplicationDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static DateTime Monday(DateTime date) => date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    [HttpGet, Authorize(Roles="Employee")]
    public async Task<IActionResult> Mine() { var rows=await db.WeeklyTimesheets.AsNoTracking().Include(x=>x.Lines).Include(x=>x.ReviewedBy).Where(x=>x.EmployeeId==UserId).OrderByDescending(x=>x.WeekStartDate).ToListAsync(); return Ok(rows.Select(x=>new{x.Id,x.WeekStartDate,x.WeekEndDate,x.Status,x.TotalHours,billableHours=x.Lines.Where(l=>l.BillingType=="Billable").Sum(l=>l.TotalHours),nonBillableHours=x.Lines.Where(l=>l.BillingType=="Non-billable").Sum(l=>l.TotalHours),x.SubmittedAt,reviewedBy=x.ReviewedBy?.FullName,x.ReviewedAt,x.ManagerRemarks})); }

    [HttpGet("current"), Authorize(Roles="Employee")]
    public async Task<IActionResult> Current(DateTime? weekStart) { var current = Monday(DateTime.UtcNow); var week = Monday(weekStart ?? current); if (week > current) return BadRequest(new { message = "Future timesheets are not permitted" }); var required=await RequiredWeeklyHours(); var row = await Query().FirstOrDefaultAsync(x => x.EmployeeId == UserId && x.WeekStartDate == week); return row is null ? Ok(new { weekStartDate = week, weekEndDate = week.AddDays(4), status = "Draft", requiredWeeklyHours=required, lines = Array.Empty<object>() }) : Ok(ToResponse(row,required)); }

    [HttpGet("{id:int}"), Authorize(Roles="Employee,Manager")]
    public async Task<IActionResult> Get(int id) { var row = await Query().FirstOrDefaultAsync(x => x.Id == id); if (row is null || (User.IsInRole("Employee") ? row.EmployeeId != UserId : row.ManagerId != UserId)) return NotFound(new { message = "Timesheet not found" }); return Ok(ToResponse(row)); }

    [HttpPost, Authorize(Roles="Employee")]
    public async Task<IActionResult> Create(SaveWeeklyTimesheetDto dto)
    {
        var week = Monday(dto.WeekStartDate); if (dto.WeekStartDate.Date != week) return BadRequest(new { message = "Week must start on Monday" });
        if (await db.WeeklyTimesheets.AnyAsync(x => x.EmployeeId == UserId && x.WeekStartDate == week)) return Conflict(new { message = "A timesheet already exists for this week" });
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == UserId && x.IsActive && x.Role == "Employee");
        if (user?.ManagerId is null || !await db.Users.AnyAsync(x => x.UserId == user.ManagerId && x.IsActive && x.Role == "Manager")) return BadRequest(new { message = "An active assigned Manager is required" });
        var sheet = new WeeklyTimesheet { EmployeeId = UserId, ManagerId = user.ManagerId.Value, WeekStartDate = week, WeekEndDate = week.AddDays(4) };
        var error = await ReplaceLines(sheet, dto.Lines); if (error is not null) return BadRequest(new { message = error });
        db.WeeklyTimesheets.Add(sheet); await db.SaveChangesAsync(); return Ok(ToResponse(sheet));
    }

    [HttpPut("{id:int}"), Authorize(Roles="Employee")]
    public async Task<IActionResult> Update(int id, SaveWeeklyTimesheetDto dto)
    {
        var sheet = await Query().FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == UserId); if (sheet is null) return NotFound(new { message = "Timesheet not found" });
        if (sheet.Status is not ("Draft" or "Rejected")) return Conflict(new { message = "Submitted or approved timesheets are read-only" });
        var error = await ReplaceLines(sheet, dto.Lines); if (error is not null) return BadRequest(new { message = error });
        sheet.Status = "Draft"; sheet.ManagerRemarks = null; sheet.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync(); return Ok(ToResponse(sheet));
    }

    [HttpPost("{id:int}/submit"), Authorize(Roles="Employee")]
    public async Task<IActionResult> Submit(int id)
    {
        var sheet = await Query().FirstOrDefaultAsync(x => x.Id == id && x.EmployeeId == UserId); if (sheet is null) return NotFound(new { message = "Timesheet not found" });
        if (sheet.Status is not ("Draft" or "Rejected")) return Conflict(new { message = "Timesheet has already been submitted" });
        if (sheet.Lines.Count == 0 || sheet.TotalHours <= 0) return BadRequest(new { message = "Enter work hours before submitting" });
        sheet.Status = "Submitted"; sheet.SubmittedAt = DateTime.UtcNow; sheet.UpdatedAt = DateTime.UtcNow;
        var employee = await db.Users.Where(x => x.UserId == UserId).Select(x => x.FullName).FirstAsync();
        db.Notifications.Add(new Notification { UserId = sheet.ManagerId, Title = "Weekly timesheet submitted", Message = $"{employee} submitted {sheet.TotalHours:0.##} hours for {sheet.WeekStartDate:dd MMM} - {sheet.WeekEndDate:dd MMM}.", Type = "TimesheetSubmitted", RelatedUrl = "/manager/timesheets", DeduplicationKey = $"timesheet-submitted:{sheet.Id}:{sheet.SubmittedAt:O}" });
        await db.SaveChangesAsync(); return Ok(new { message = "Timesheet submitted" });
    }

    [HttpGet("review/pending"), Authorize(Roles="Manager")]
    public async Task<IActionResult> PendingReview() => Ok((await Query().Where(x => x.ManagerId == UserId && x.Status == "Submitted").OrderBy(x => x.WeekStartDate).ToListAsync()).Select(x=>ToResponse(x)));

    [HttpPost("{id:int}/review"), Authorize(Roles="Manager")]
    public async Task<IActionResult> Review(int id, ReviewWeeklyTimesheetDto dto)
    {
        var decision = dto.Decision.Trim(); if (decision is not ("Approved" or "Rejected")) return BadRequest(new { message = "Decision must be Approved or Rejected" });
        var sheet = await db.WeeklyTimesheets.FirstOrDefaultAsync(x => x.Id == id && x.ManagerId == UserId); if (sheet is null) return NotFound(new { message = "Timesheet not found" });
        if (sheet.Status != "Submitted") return Conflict(new { message = "Only submitted timesheets can be reviewed" });
        if (decision == "Rejected" && string.IsNullOrWhiteSpace(dto.Remarks)) return BadRequest(new { message = "Manager remarks are required when rejecting" });
        sheet.Status = decision; sheet.ManagerRemarks = dto.Remarks?.Trim(); sheet.ReviewedAt = DateTime.UtcNow; sheet.ReviewedById = UserId; sheet.UpdatedAt = DateTime.UtcNow;
        db.Notifications.Add(new Notification { UserId = sheet.EmployeeId, Title = $"Timesheet {decision.ToLowerInvariant()}", Message = $"Your timesheet for {sheet.WeekStartDate:dd MMM} - {sheet.WeekEndDate:dd MMM} was {decision.ToLowerInvariant()}. {sheet.ManagerRemarks}".Trim(), Type = "TimesheetReview", RelatedUrl = "/employee/timesheet", DeduplicationKey = $"timesheet-review:{sheet.Id}:{decision}" });
        await db.SaveChangesAsync(); return Ok(new { message = $"Timesheet {decision.ToLowerInvariant()}" });
    }

    private IQueryable<WeeklyTimesheet> Query() => db.WeeklyTimesheets.Include(x => x.Employee).Include(x => x.ReviewedBy).Include(x => x.Lines).ThenInclude(x => x.Project).Include(x => x.Lines).ThenInclude(x => x.Task);
    private static object ToResponse(WeeklyTimesheet x, decimal requiredWeeklyHours = 45) => new { x.Id, x.EmployeeId, employee = x.Employee?.FullName, x.ManagerId, x.WeekStartDate, x.WeekEndDate, x.Status, x.SubmittedAt, x.ReviewedAt, x.ReviewedById, x.ManagerRemarks, x.TotalHours, x.CreatedAt, x.UpdatedAt, requiredWeeklyHours, billableHours=x.Lines.Where(l=>l.BillingType=="Billable").Sum(l=>l.TotalHours), nonBillableHours=x.Lines.Where(l=>l.BillingType=="Non-billable").Sum(l=>l.TotalHours), reviewedBy=x.ReviewedBy?.FullName, lines = x.Lines.Select(l => new { l.Id, l.ProjectId, project = l.Project?.ProjectName, l.TaskId, task = l.Task?.Title, l.WorkDescription, l.BillingType, l.MondayHours, l.TuesdayHours, l.WednesdayHours, l.ThursdayHours, l.FridayHours, l.TotalHours }) };

    private async Task<decimal> RequiredWeeklyHours()
    {
        var values=await db.SystemSettings.Where(x=>x.SettingKey=="StandardWorkHours"||x.SettingKey=="WorkingDays").ToDictionaryAsync(x=>x.SettingKey,x=>x.SettingValue);
        var hours=values.TryGetValue("StandardWorkHours",out var raw)&&decimal.TryParse(raw,out var parsed)&&parsed>0?parsed:9;
        var days=values.TryGetValue("WorkingDays",out var working)&&!string.IsNullOrWhiteSpace(working)?working.Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Length:5;
        return hours*Math.Clamp(days,1,7);
    }

    private async Task<string?> ReplaceLines(WeeklyTimesheet sheet, List<WeeklyTimesheetLineDto> lines)
    {
        if (lines.Count == 0) return "Add at least one timesheet row"; if (lines.Count > 50) return "A maximum of 50 rows is allowed";
        var duplicates = lines.GroupBy(x => new { x.ProjectId, x.TaskId, Description = x.WorkDescription?.Trim().ToLowerInvariant() }).Any(x => x.Count() > 1); if (duplicates) return "Duplicate project/task rows are not allowed";
        var projectIds = lines.Select(x => x.ProjectId).Distinct().ToList(); var assigned = await db.ProjectAssignments.Where(x => x.UserId == UserId && projectIds.Contains(x.ProjectId) && !x.Project!.IsArchived).Select(x => x.ProjectId).ToListAsync(); if (assigned.Count != projectIds.Count) return "Only assigned active projects can be selected";
        var taskIds = lines.Where(x => x.TaskId.HasValue).Select(x => x.TaskId!.Value).Distinct().ToList(); var validTasks = await db.WorkTasks.Where(x => taskIds.Contains(x.TaskId) && x.AssignedToId == UserId && x.Status != "Cancelled").Select(x => new { x.TaskId, x.ProjectId }).ToListAsync(); if (validTasks.Count != taskIds.Count || lines.Any(x => x.TaskId.HasValue && !validTasks.Any(t => t.TaskId == x.TaskId && t.ProjectId == x.ProjectId))) return "Tasks must be active, assigned to you, and belong to the selected project";
        if (lines.Any(x => !x.TaskId.HasValue && string.IsNullOrWhiteSpace(x.WorkDescription))) return "Work description is required when no task is selected";
        if (lines.Any(x => x.BillingType is not ("Billable" or "Non-billable"))) return "Billing type must be Billable or Non-billable";
        decimal[] Hours(WeeklyTimesheetLineDto x) => [x.MondayHours,x.TuesdayHours,x.WednesdayHours,x.ThursdayHours,x.FridayHours]; if (lines.Any(x => Hours(x).Any(h => h < 0 || h > 24))) return "Daily hours must be between 0 and 24";
        if (sheet.Id != 0) db.WeeklyTimesheetLines.RemoveRange(sheet.Lines); sheet.Lines.Clear();
        foreach (var x in lines) { var h = Hours(x); sheet.Lines.Add(new WeeklyTimesheetLine { ProjectId=x.ProjectId,TaskId=x.TaskId,WorkDescription=x.WorkDescription?.Trim(),BillingType=x.BillingType,MondayHours=h[0],TuesdayHours=h[1],WednesdayHours=h[2],ThursdayHours=h[3],FridayHours=h[4],TotalHours=h.Sum() }); }
        sheet.TotalHours = sheet.Lines.Sum(x => x.TotalHours); return null;
    }
}
