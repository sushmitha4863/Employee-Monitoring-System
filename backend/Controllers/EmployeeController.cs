using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers;

[ApiController, Route("api/employee"), Authorize(Roles = "Employee")]
public sealed class EmployeeController(ApplicationDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var id = UserId; var now = DateTime.UtcNow; var soon = now.AddDays(7);
        var projects = await db.ProjectAssignments.AsNoTracking().Where(x => x.UserId == id && !x.Project!.IsArchived)
            .Select(x => new { x.ProjectId, x.Project!.ProjectName, x.Project.ProjectCode, x.Project.Status, x.Project.Priority, x.Project.ProgressPercentage, x.Project.EndDate }).ToListAsync();
        var tasks = await db.WorkTasks.AsNoTracking().Where(x => x.AssignedToId == id && !x.Project!.IsArchived)
            .Select(x => new { x.TaskId, x.Title, ProjectName = x.Project != null ? x.Project.ProjectName : null, x.Status, x.Priority, x.ProgressPercentage, x.DueDate }).ToListAsync();
        var balance = await db.LeaveBalances.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id);
        var notices = await db.Notifications.AsNoTracking().Where(x => x.UserId == id).OrderByDescending(x => x.CreatedAt).Take(5)
            .Select(x => new { notificationId = x.Id, x.Title, x.Message, x.Type, x.RelatedUrl, x.IsRead, x.CreatedAt }).ToListAsync();
        var meetings = await db.CalendarEvents.AsNoTracking().Where(x => (x.UserId == id || x.CreatedById == id) && x.Status != "Cancelled" && x.StartTime >= now).OrderBy(x => x.StartTime).Take(5)
            .Select(x => new { x.EventId, x.Title, x.StartTime, x.EndTime, x.Location, x.Status }).ToListAsync();
        return Ok(new { assignedProjects = projects.Count, pendingTasks = tasks.Count(x => x.Status != "Completed" && x.Status != "Cancelled"), completedTasks = tasks.Count(x => x.Status == "Completed"), leaveBalance = balance == null ? 0 : balance.TotalLeaves - balance.UsedLeaves, unreadNotifications = await db.Notifications.CountAsync(x => x.UserId == id && !x.IsRead), tasksDueSoon = tasks.Count(x => x.DueDate >= now && x.DueDate <= soon && x.Status != "Completed"), activeProjects = projects.Where(x => x.Status != "Completed").Take(5), pendingTaskItems = tasks.Where(x => x.Status != "Completed" && x.Status != "Cancelled").OrderBy(x => x.DueDate).Take(5), upcomingDeadlines = tasks.Where(x => x.DueDate >= now).OrderBy(x => x.DueDate).Take(5), upcomingMeetings = meetings, recentNotifications = notices });
    }

    [HttpGet("projects")]
    public async Task<IActionResult> Projects()
    {
        var id = UserId;
        var rows = await db.ProjectAssignments.AsNoTracking().Where(x => x.UserId == id && !x.Project!.IsArchived)
            .Select(x => new { x.ProjectId, x.Project!.ProjectName, x.Project.ProjectCode, x.Project.Description, Manager = x.Project.Manager!.FullName, Department = x.Project.Department!.Name, x.Project.StartDate, Deadline = x.Project.EndDate, x.Project.Priority, x.Project.Status, x.Project.ProgressPercentage,
                ContributionProgress = db.WorkTasks.Where(t => t.AssignedToId == id && t.ProjectId == x.ProjectId).Select(t => (double?)t.ProgressPercentage).Average() ?? 0,
                Tasks = db.WorkTasks.Where(t => t.AssignedToId == id && t.ProjectId == x.ProjectId).Select(t => new { t.TaskId, t.Title, t.Status, t.ProgressPercentage, t.DueDate }).ToList() }).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("projects/{projectId:int}")]
    public async Task<IActionResult> Project(int projectId)
    {
        if (!await db.ProjectAssignments.AnyAsync(x => x.UserId == UserId && x.ProjectId == projectId)) return NotFound(new { message = "Project not found" });
        return await Projects();
    }

    [HttpGet("tasks")]
    public async Task<IActionResult> Tasks()
    {
        var id = UserId;
        return Ok(await db.WorkTasks.AsNoTracking().Where(x => x.AssignedToId == id)
            .Select(x => new { x.TaskId, x.Title, x.Description, x.ProjectId, Project = x.Project!.ProjectName, ProjectArchived = x.Project != null && x.Project.IsArchived, AssignedBy = x.AssignedBy!.FullName, AssignedDate = x.CreatedAt, StartDate = x.StartedAt, x.DueDate, x.Priority, x.Status, x.ProgressPercentage, WorkNote = x.EmployeeRemarks, x.ManagerFeedback, x.SubmittedAt, x.CompletedAt }).OrderBy(x => x.DueDate).ToListAsync());
    }

    [HttpPatch("tasks/{taskId:int}")]
    public async Task<IActionResult> UpdateTask(int taskId, EmployeeTaskUpdateDto dto)
    {
        var task = await db.WorkTasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.TaskId == taskId && x.AssignedToId == UserId);
        if (task is null) return NotFound(new { message = "Task not found" });
        if (task.Project?.IsArchived == true || task.Status is "Cancelled" or "Completed" or "Ready for Review") return Conflict(new { message = "This task is read-only" });
        if (dto.ProgressPercentage is < 0 or > 100) return BadRequest(new { message = "Progress must be between 0 and 100" });
        var allowed = new[] { "Assigned", "In Progress", "Blocked", "Reopened" };
        if (!allowed.Contains(dto.Status)) return BadRequest(new { message = "Invalid employee task status" });
        task.ProgressPercentage = dto.ProgressPercentage; task.EmployeeRemarks = dto.WorkNote?.Trim(); task.Status = dto.Status;
        if (dto.Status == "In Progress" && task.StartedAt is null) task.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(); return Ok(new { message = "Task updated" });
    }

    [HttpPost("tasks/{taskId:int}/submit-review")]
    public async Task<IActionResult> SubmitReview(int taskId)
    {
        var task = await db.WorkTasks.Include(x => x.Project).FirstOrDefaultAsync(x => x.TaskId == taskId && x.AssignedToId == UserId);
        if (task is null) return NotFound(new { message = "Task not found" });
        if (task.Project?.IsArchived == true || task.Status is "Cancelled" or "Completed") return Conflict(new { message = "This task cannot be submitted" });
        if (task.ProgressPercentage != 100) return BadRequest(new { message = "Progress must be 100% before review" });
        task.Status = "Ready for Review"; task.SubmittedAt = DateTime.UtcNow;
        db.Notifications.Add(new Notification { UserId = task.AssignedById, Title = "Task ready for review", Message = $"{task.Title} is ready for review.", Type = "TaskReview", RelatedUrl = "/manager/tasks" });
        await db.SaveChangesAsync(); return Ok(new { message = "Task submitted for manager review" });
    }

    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar(DateTime? from, DateTime? to)
    {
        var id = UserId; var start = (from ?? DateTime.UtcNow.Date.AddMonths(-1)).Date; var end = (to ?? start.AddMonths(3)).Date.AddDays(1);
        var meetings = await db.CalendarEvents.AsNoTracking().Where(x => (x.UserId == id || x.CreatedById == id) && x.Status != "Cancelled" && x.StartTime >= start && x.StartTime < end).Select(x => new { id = "meeting-" + x.EventId, eventId=x.EventId, meetingGroupId=x.MeetingGroupId, type=x.EventType, x.Title, date=x.StartTime, x.StartTime, x.EndTime, x.Location, x.Description, x.Status, x.ProjectId, project=x.Project!.ProjectName, organizer=x.CreatedBy!.FullName, isOrganizer=x.CreatedById==id, attendeeIds=db.CalendarEvents.Where(a=>a.MeetingGroupId==x.MeetingGroupId&&a.UserId!=id).Select(a=>a.UserId).ToList(), attendees=db.CalendarEvents.Where(a=>a.MeetingGroupId==x.MeetingGroupId).Select(a=>a.User!.FullName).ToList() }).ToListAsync();
        var projects = await db.ProjectAssignments.AsNoTracking().Where(x => x.UserId == id && !x.Project!.IsArchived && x.Project.EndDate >= start && x.Project.EndDate < end).Select(x => new { id = "project-" + x.ProjectId, type = "Project deadline", title = x.Project!.ProjectName, date = x.Project.EndDate, startTime = (DateTime?)null, endTime = (DateTime?)null, location = (string?)null, description = x.Project.Description, status = x.Project.Status, project = x.Project.ProjectName }).ToListAsync();
        var tasks = await db.WorkTasks.AsNoTracking().Where(x => x.AssignedToId == id && x.DueDate >= start && x.DueDate < end && x.Status != "Cancelled").Select(x => new { id = "task-" + x.TaskId, type = "Task deadline", title = x.Title, date = x.DueDate!.Value, startTime = (DateTime?)null, endTime = (DateTime?)null, location = (string?)null, description = x.Description, status = x.Status, project = x.Project!.ProjectName }).ToListAsync();
        var holidays = await db.CompanyHolidays.AsNoTracking().Where(x => x.IsActive && x.HolidayDate >= start && x.HolidayDate < end).Select(x => new { id = "holiday-" + x.HolidayId, type = "Holiday", title = x.HolidayName, date = x.HolidayDate, startTime = (DateTime?)null, endTime = (DateTime?)null, location = (string?)null, description = x.Description, status = "Active", project = (string?)null }).ToListAsync();
        return Ok(meetings.Cast<object>().Concat(projects).Concat(tasks).Concat(holidays));
    }

    [HttpGet("meeting-options")]
    public async Task<IActionResult> MeetingOptions(int? projectId)
    {
        var employee = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == UserId && x.IsActive);
        if (employee is null) return Unauthorized();
        var allowedIds = new HashSet<int>();
        if (employee.ManagerId.HasValue) allowedIds.Add(employee.ManagerId.Value);
        var myProjects = await db.ProjectAssignments.Where(x => x.UserId == UserId && !x.Project!.IsArchived).Select(x => x.ProjectId).ToListAsync();
        if (projectId.HasValue && !myProjects.Contains(projectId.Value)) return NotFound(new { message = "Project not found" });
        var scope = projectId.HasValue ? new List<int> { projectId.Value } : myProjects;
        var teammateIds = await db.ProjectAssignments.Where(x => scope.Contains(x.ProjectId) && x.UserId != UserId && x.User!.IsActive).Select(x => x.UserId).Distinct().ToListAsync();
        foreach (var id in teammateIds) allowedIds.Add(id);
        var people = await db.Users.AsNoTracking().Where(x => allowedIds.Contains(x.UserId) && x.IsActive).Select(x => new { x.UserId, x.FullName, x.Role, x.EmployeeCode }).OrderBy(x => x.FullName).ToListAsync();
        var projects = await db.Projects.AsNoTracking().Where(x => myProjects.Contains(x.ProjectId) && !x.IsArchived).Select(x => new { x.ProjectId, x.ProjectName, x.ProjectCode }).ToListAsync();
        return Ok(new { people, projects });
    }

    [HttpPost("meetings")]
    public async Task<IActionResult> ScheduleMeeting(CreateCalendarEventDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { message = "Meeting title is required" });
        if (dto.EndTime <= dto.StartTime) return BadRequest(new { message = "Meeting end time must be after start time" });
        if (dto.UserIds.Count == 0 || dto.UserIds.Count != dto.UserIds.Distinct().Count()) return BadRequest(new { message = "Select unique permitted attendees" });
        var options = await AllowedAttendees(dto.ProjectId); if (options.Error is not null) return options.Error;
        if (dto.UserIds.Any(x => !options.Ids.Contains(x))) return Forbid();
        var zone = await CompanyZone(); var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dto.StartTime, DateTimeKind.Unspecified), zone); var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dto.EndTime, DateTimeKind.Unspecified), zone);
        var policy = await MeetingPolicy.ValidateAsync(db, UserId, dto.Title, start, end, dto.UserIds); if (policy is not null) return StatusCode(policy.StatusCode, new { message = policy.Message });
        var group = Guid.NewGuid().ToString("N"); var attendees = dto.UserIds.Distinct().ToList();
        foreach (var attendee in attendees) db.CalendarEvents.Add(new CalendarEvent { Title=dto.Title.Trim(),Description=dto.Description?.Trim(),EventType=string.IsNullOrWhiteSpace(dto.EventType)?"Meeting":dto.EventType,StartTime=start,EndTime=end,UserId=attendee,CreatedById=UserId,Location=dto.Location?.Trim(),ProjectId=dto.ProjectId,MeetingGroupId=group,Status="Scheduled" });
        foreach (var attendee in dto.UserIds) db.Notifications.Add(new Notification { UserId=attendee,Title="Meeting invitation",Message=$"{dto.Title.Trim()} is scheduled for {dto.StartTime:dd MMM yyyy HH:mm}.",Type="MeetingInvitation",RelatedUrl="/employee/calendar",DeduplicationKey=$"meeting-invite:{group}:{attendee}" });
        await db.SaveChangesAsync(); return Ok(new { message = "Meeting scheduled", meetingGroupId = group });
    }

    [HttpPut("meetings/{eventId:int}")]
    public async Task<IActionResult> UpdateMeeting(int eventId, CreateCalendarEventDto dto)
    {
        var seed = await db.CalendarEvents.FirstOrDefaultAsync(x => x.EventId == eventId && x.CreatedById == UserId); if (seed is null) return NotFound(new { message = "Meeting not found" });
        if (seed.Status == "Cancelled") return Conflict(new { message = "Cancelled meetings cannot be edited" });
        if (dto.EndTime <= dto.StartTime) return BadRequest(new { message = "Meeting end time must be after start time" });
        var options = await AllowedAttendees(dto.ProjectId); if (options.Error is not null) return options.Error; if (dto.UserIds.Count != dto.UserIds.Distinct().Count() || dto.UserIds.Any(x => !options.Ids.Contains(x))) return Forbid();
        var rows = await db.CalendarEvents.Where(x => x.MeetingGroupId == seed.MeetingGroupId).ToListAsync(); var zone = await CompanyZone(); var start=TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dto.StartTime,DateTimeKind.Unspecified),zone);var end=TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(dto.EndTime,DateTimeKind.Unspecified),zone);var policy=await MeetingPolicy.ValidateAsync(db,UserId,dto.Title,start,end,dto.UserIds,seed.MeetingGroupId);if(policy is not null)return StatusCode(policy.StatusCode,new{message=policy.Message});
        db.CalendarEvents.RemoveRange(rows.Where(x => !dto.UserIds.Contains(x.UserId))); var existing=rows.Select(x=>x.UserId).ToHashSet(); foreach(var attendee in dto.UserIds.Distinct().Where(x=>!existing.Contains(x)))db.CalendarEvents.Add(new CalendarEvent{Title=dto.Title.Trim(),Description=dto.Description?.Trim(),EventType=dto.EventType,StartTime=start,EndTime=end,UserId=attendee,CreatedById=UserId,Location=dto.Location?.Trim(),ProjectId=dto.ProjectId,MeetingGroupId=seed.MeetingGroupId,Status="Scheduled"});
        foreach(var row in rows.Where(x=>dto.UserIds.Contains(x.UserId))){row.Title=dto.Title.Trim();row.Description=dto.Description?.Trim();row.EventType=dto.EventType;row.StartTime=start;row.EndTime=end;row.Location=dto.Location?.Trim();row.ProjectId=dto.ProjectId;row.UpdatedAt=DateTime.UtcNow;if(row.UserId!=UserId)db.Notifications.Add(new Notification{UserId=row.UserId,Title="Meeting updated",Message=$"{dto.Title.Trim()} was updated.",Type="MeetingUpdated",RelatedUrl="/employee/calendar",DeduplicationKey=$"meeting-update:{seed.MeetingGroupId}:{row.UserId}:{row.UpdatedAt.Ticks}"});}
        await db.SaveChangesAsync(); return Ok(new { message = "Meeting updated" });
    }

    [HttpPatch("meetings/{eventId:int}/cancel")]
    public async Task<IActionResult> CancelMeeting(int eventId)
    {
        var seed=await db.CalendarEvents.FirstOrDefaultAsync(x=>x.EventId==eventId&&x.CreatedById==UserId);if(seed is null)return NotFound(new{message="Meeting not found"});if(seed.Status=="Cancelled")return Conflict(new{message="Meeting is already cancelled"});var rows=await db.CalendarEvents.Where(x=>x.MeetingGroupId==seed.MeetingGroupId).ToListAsync();foreach(var row in rows){row.Status="Cancelled";row.UpdatedAt=DateTime.UtcNow;if(row.UserId!=UserId)db.Notifications.Add(new Notification{UserId=row.UserId,Title="Meeting cancelled",Message=$"{row.Title} was cancelled.",Type="MeetingCancelled",RelatedUrl="/employee/calendar",DeduplicationKey=$"meeting-cancel:{seed.MeetingGroupId}:{row.UserId}"});}await db.SaveChangesAsync();return Ok(new{message="Meeting cancelled"});
    }

    private async Task<(HashSet<int> Ids, IActionResult? Error)> AllowedAttendees(int? projectId)
    {
        var employee=await db.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId==UserId&&x.IsActive);if(employee is null)return(new(),Unauthorized());var projectIds=await db.ProjectAssignments.Where(x=>x.UserId==UserId&&!x.Project!.IsArchived).Select(x=>x.ProjectId).ToListAsync();if(projectId.HasValue&&!projectIds.Contains(projectId.Value))return(new(),NotFound(new{message="Project not found"}));var scope=projectId.HasValue?new List<int>{projectId.Value}:projectIds;var ids=(await db.ProjectAssignments.Where(x=>scope.Contains(x.ProjectId)&&x.UserId!=UserId&&x.User!.IsActive).Select(x=>x.UserId).Distinct().ToListAsync()).ToHashSet();if(employee.ManagerId.HasValue)ids.Add(employee.ManagerId.Value);return(ids,null);
    }
    private async Task<TimeZoneInfo> CompanyZone(){var name=await db.SystemSettings.Where(x=>x.SettingKey=="CompanyTimezone").Select(x=>x.SettingValue).FirstOrDefaultAsync()??"India Standard Time";try{return TimeZoneInfo.FindSystemTimeZoneById(name);}catch{return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");}}

    [HttpGet("performance")]
    public async Task<IActionResult> Performance(int? year, int? quarter)
    {
        var id = UserId; var y = year ?? DateTime.UtcNow.Year; var q = Math.Clamp(quarter ?? ((DateTime.UtcNow.Month - 1) / 3 + 1), 1, 4); var start = new DateTime(y, (q - 1) * 3 + 1, 1); var end = start.AddMonths(3);
        var attendance = await db.Attendances.AsNoTracking().Where(x => x.UserId == id && x.AttendanceDate >= start && x.AttendanceDate < end).ToListAsync();
        var tasks = await db.WorkTasks.AsNoTracking().Where(x => x.AssignedToId == id && x.CreatedAt < end).ToListAsync();
        var projectIds = await db.ProjectAssignments.AsNoTracking().Where(x => x.UserId == id).Select(x => x.ProjectId).ToListAsync();
        var sheets = await db.WeeklyTimesheets.AsNoTracking().Include(x => x.Lines).ThenInclude(x=>x.Project).Where(x => x.EmployeeId == id && x.WeekStartDate >= start && x.WeekStartDate < end && x.Status == "Approved").ToListAsync();
        var review = await db.PerformanceReviews.AsNoTracking().Where(x => x.EmployeeId == id && x.Year == y && x.Quarter == $"Q{q}" && x.IsReleased).OrderByDescending(x => x.ReviewDate).FirstOrDefaultAsync();
        var completed = tasks.Count(x => x.Status == "Completed"); var onTime = tasks.Count(x => x.Status == "Completed" && (!x.DueDate.HasValue || x.CompletedAt <= x.DueDate));
        return Ok(new { year = y, quarter = q, attendanceSummary = new { presentDays = attendance.Count(x => x.Status == "Present" || x.Status == "Late"), lateDays = attendance.Count(x => x.LateMinutes > 0), approvedLeaveDays = await db.Leaves.CountAsync(x => x.UserId == id && x.Status == "Approved" && x.FromDate < end && x.ToDate >= start), missingCheckoutCount = attendance.Count(x => x.CheckInTime != null && x.CheckOutTime == null), averageWorkingHours = attendance.Count == 0 ? 0 : Math.Round(attendance.Average(x => x.EffectiveHours), 2) }, taskPerformance = new { assignedTasks = tasks.Count, completedTasks = completed, pendingTasks = tasks.Count(x => x.Status != "Completed" && x.Status != "Cancelled"), overdueTasks = tasks.Count(x => x.DueDate < DateTime.UtcNow && x.Status != "Completed" && x.Status != "Cancelled"), readyForReview = tasks.Count(x => x.Status == "Ready for Review"), reopenedTasks = tasks.Count(x => x.Status == "Reopened"), completionRate = tasks.Count == 0 ? 0 : Math.Round(completed * 100d / tasks.Count, 1), averageProgress = tasks.Count == 0 ? 0 : Math.Round(tasks.Average(x => x.ProgressPercentage), 1), onTimeCompletionRate = completed == 0 ? 0 : Math.Round(onTime * 100d / completed, 1) }, projectPerformance = new { assignedProjects = projectIds.Count, activeProjects = await db.Projects.CountAsync(x => projectIds.Contains(x.ProjectId) && !x.IsArchived && x.Status != "Completed"), completedContributions = completed, contributionProgress = tasks.Count == 0 ? 0 : Math.Round(tasks.Average(x => x.ProgressPercentage), 1), totalTimesheetHours = sheets.Sum(x => x.TotalHours), billableHours = sheets.SelectMany(x => x.Lines).Where(x => x.BillingType == "Billable").Sum(x => x.TotalHours), nonBillableHours = sheets.SelectMany(x => x.Lines).Where(x => x.BillingType == "Non-billable").Sum(x => x.TotalHours), timesheetHoursByProject=sheets.SelectMany(x=>x.Lines).GroupBy(x=>x.Project!.ProjectName).Select(x=>new{project=x.Key,hours=x.Sum(l=>l.TotalHours)}), managerFeedback=tasks.Where(x=>!string.IsNullOrEmpty(x.ManagerFeedback)).OrderByDescending(x=>x.CompletedAt).Select(x=>x.ManagerFeedback).FirstOrDefault() }, managerEvaluation = review == null ? null : new { review.WorkQuality, review.Timeliness, review.Collaboration, review.Communication, review.TechnicalContribution, review.ProjectContribution, review.Strengths, review.ImprovementAreas, finalRemarks = review.ManagerRemarks, review.ReviewDate } });
    }
}


