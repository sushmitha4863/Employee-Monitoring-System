
using ClosedXML.Excel;
using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Manager")]
    public class ManagerPortalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        public ManagerPortalController(ApplicationDbContext context, EmailService emailService) { _context = context; _emailService = emailService; }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var managerId = CurrentUserId(); var teamIds = await TeamUserIds(managerId); var today = (await CompanyLocalNow()).Date; var monthStart = new DateTime(today.Year, today.Month, 1); var weekStart = today.AddDays(-(int)today.DayOfWeek + 1);
            var attendance = await _context.Attendances.Where(x => teamIds.Contains(x.UserId) && x.AttendanceDate >= monthStart).ToListAsync();
            var todayAttendance = attendance.Where(x => x.AttendanceDate.Date == today).ToList();
            var leaves = await _context.Leaves.Where(x => teamIds.Contains(x.UserId)).ToListAsync();
            var tasks = await _context.WorkTasks.Include(x => x.AssignedTo).Where(x => teamIds.Contains(x.AssignedToId)).ToListAsync();
            var projects = await ManagerProjects(managerId).ToListAsync();
            var timesheets = await _context.WeeklyTimesheets.Where(x => x.ManagerId == managerId).ToListAsync();
            var meetings = await _context.CalendarEvents.Where(x => x.CreatedById == managerId && x.StartTime >= DateTime.UtcNow).OrderBy(x => x.StartTime).Take(8).ToListAsync();
            return Ok(new { teamMembers = teamIds.Count, pendingTasks = tasks.Count(x => x.Status is "Assigned" or "Pending" or "In Progress" or "Blocked" or "Reopened"), tasksAwaitingReview = tasks.Count(x => x.Status == "Ready for Review"), unreadNotifications = await _context.Notifications.CountAsync(x => x.UserId == managerId && !x.IsRead), projectsRequiringAttention = projects.Where(x => !x.IsArchived && (x.Status == "Delayed" || x.Status == "At Risk" || (x.EndDate.Date <= today.AddDays(14) && x.Status != "Completed"))).Select(x => new { x.ProjectId, x.ProjectName, x.ProjectCode, x.Status, x.ProgressPercentage, Deadline = x.EndDate, x.Priority }), tasksAwaitingReviewItems = tasks.Where(x => x.Status == "Ready for Review").Select(x => new { x.TaskId, x.Title, x.Status, x.DueDate, EmployeeName = x.AssignedTo != null ? x.AssignedTo.FullName : null }), teamSize = teamIds.Count, presentToday = todayAttendance.Count(x => x.AttendanceStatus == "Present" || x.Status == "Present"), absentToday = Math.Max(0, teamIds.Count - todayAttendance.Count), onLeave = leaves.Count(x => x.Status == "Approved" && x.FromDate.Date <= today && x.ToDate.Date >= today), lateEmployees = todayAttendance.Count(x => x.LateMinutes > 0 || x.AttendanceStatus == "Late"), pendingLeaveRequests = leaves.Count(x => x.Status == "Pending"), pendingTimesheets = timesheets.Count(x => x.Status == "Submitted"), activeProjects = projects.Count(x => !x.IsArchived && x.Status != "Completed"), tasksDueToday = tasks.Count(x => x.DueDate.HasValue && x.DueDate.Value.Date == today), completedTasks = tasks.Count(x => x.Status == "Completed"), teamProductivity = tasks.Count == 0 ? 0 : Math.Round((double)tasks.Count(x => x.Status == "Completed") / tasks.Count * 100), weeklyHours = Math.Round(attendance.Where(x => x.AttendanceDate >= weekStart).Sum(x => x.EffectiveHours > 0 ? x.EffectiveHours : x.TotalHours), 2), monthlyHours = Math.Round(attendance.Sum(x => x.EffectiveHours > 0 ? x.EffectiveHours : x.TotalHours), 2), attendanceTrend = attendance.GroupBy(x => x.AttendanceDate.Date).OrderBy(x => x.Key).Select(x => new { date = x.Key, present = x.Count(a => a.AttendanceStatus == "Present" || a.Status == "Present"), late = x.Count(a => a.LateMinutes > 0), absent = Math.Max(0, teamIds.Count - x.Count()) }), projectProgress = projects.Select(x => new { x.ProjectId, x.ProjectName, x.ProgressPercentage, x.Status, x.EndDate }), upcomingMeetings = meetings, upcomingDeadlines = tasks.Where(x => x.DueDate.HasValue && x.DueDate.Value.Date >= today).OrderBy(x => x.DueDate).Take(8).Select(x => new { x.TaskId, x.Title, x.DueDate, EmployeeName = x.AssignedTo != null ? x.AssignedTo.FullName : null }), recentNotifications = await _context.Notifications.Where(x => x.UserId == managerId).OrderByDescending(x => x.CreatedAt).Take(10).ToListAsync(), announcements = await TeamAnnouncements(managerId) });
        }

        [HttpGet("team")]
        public async Task<IActionResult> Team([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? sort = "name", [FromQuery] int? projectId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100); if (projectId.HasValue && !await ManagerProjects(CurrentUserId()).AnyAsync(x => x.ProjectId == projectId && !x.IsArchived)) return NotFound(new { message = "Project not found" });
            var query = _context.Users.AsNoTracking().Include(x => x.Department).Where(x => x.ManagerId == CurrentUserId() && x.Role == "Employee"); if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.FullName.Contains(search) || x.Email.Contains(search) || (x.EmployeeCode != null && x.EmployeeCode.Contains(search))); if (!string.IsNullOrWhiteSpace(status)) query = status == "Active" ? query.Where(x => x.IsActive) : status == "Inactive" ? query.Where(x => !x.IsActive) : query; if (projectId.HasValue) query = query.Where(x => _context.ProjectAssignments.Any(a => a.ProjectId == projectId && a.UserId == x.UserId));
            var total = await query.CountAsync(); query = sort?.ToLowerInvariant() switch { "code" => query.OrderBy(x => x.EmployeeCode), "department" => query.OrderBy(x => x.Department!.Name).ThenBy(x => x.FullName), _ => query.OrderBy(x => x.FullName) }; var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(); var ids = users.Select(x => x.UserId).ToList(); var assignments = await _context.ProjectAssignments.AsNoTracking().Include(x => x.Project).Where(x => ids.Contains(x.UserId) && !x.Project!.IsArchived).ToListAsync(); var taskCounts = await _context.WorkTasks.AsNoTracking().Where(x => ids.Contains(x.AssignedToId) && x.Status != "Completed" && x.Status != "Cancelled").GroupBy(x => x.AssignedToId).ToDictionaryAsync(x => x.Key, x => x.Count());
            var items = users.Select(x => new { x.UserId, x.EmployeeCode, x.FullName, x.Email, Department = x.Department != null ? x.Department.Name : null, AccountStatus = x.IsActive ? "Active" : "Inactive", AssignedProjects = assignments.Where(a => a.UserId == x.UserId).Select(a => new { a.ProjectId, a.Project!.ProjectName, a.Project.ProjectCode }), ActiveTasks = taskCounts.GetValueOrDefault(x.UserId) }); return Ok(new { items, page, pageSize, totalCount = total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
        }        [HttpGet("team/{employeeId}")]
        public async Task<IActionResult> TeamMemberDetails(int employeeId)
        {
            var employee = await _context.Users.AsNoTracking().Include(x => x.Department).FirstOrDefaultAsync(x => x.UserId == employeeId && x.ManagerId == CurrentUserId() && x.Role == "Employee"); if (employee == null) return NotFound(new { message = "Employee not found" });
            var projects = await _context.ProjectAssignments.AsNoTracking().Include(x => x.Project).Where(x => x.UserId == employeeId && !x.Project!.IsArchived).Select(x => new { x.ProjectId, x.Project!.ProjectName, x.Project.ProjectCode, x.Project.Status, x.Project.ProgressPercentage }).ToListAsync(); var tasks = await _context.WorkTasks.AsNoTracking().Where(x => x.AssignedToId == employeeId).OrderBy(x => x.DueDate).Select(x => new { x.TaskId, x.Title, x.Status, x.Priority, x.ProgressPercentage, x.DueDate, x.ProjectId }).ToListAsync();
            return Ok(new { employee = new { employee.UserId, employee.EmployeeCode, employee.FullName, employee.Email, Department = employee.Department != null ? employee.Department.Name : null, AccountStatus = employee.IsActive ? "Active" : "Inactive" }, projects, tasks, performanceReviewAvailable = await _context.PerformanceReviews.AnyAsync(x => x.EmployeeId == employeeId) });
        }        [HttpGet("attendance")]
        public async Task<IActionResult> Attendance([FromQuery] DateTime? date, [FromQuery] int? employeeId, [FromQuery] string? status)
        {
            var ids = await TeamUserIds(CurrentUserId()); if (employeeId.HasValue && !ids.Contains(employeeId.Value)) return NotFound(new { message = "Employee not found" }); var selected = employeeId.HasValue ? new List<int> { employeeId.Value } : ids; var day = (date ?? DateTime.UtcNow).Date; var query = _context.Attendances.AsNoTracking().Include(x => x.User).Where(x => selected.Contains(x.UserId) && x.AttendanceDate.Date == day); if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.AttendanceStatus == status || x.Status == status);
            return Ok(await query.OrderBy(x => x.User!.FullName).Select(x => new { x.AttendanceId, x.UserId, EmployeeName = x.User!.FullName, x.AttendanceDate, x.CheckInTime, x.CheckOutTime, WorkingHours = x.EffectiveHours > 0 ? x.EffectiveHours : x.TotalHours, x.BreakMinutes, x.LateMinutes, MissingCheckout = x.CheckInTime != null && x.CheckOutTime == null, Status = x.AttendanceStatus ?? x.Status }).ToListAsync());
        }        [HttpGet("leaves")]
        public async Task<IActionResult> Leaves([FromQuery] string? status)
        {
            var ids = await TeamUserIds(CurrentUserId()); var query = _context.Leaves.Include(x => x.User).Where(x => ids.Contains(x.UserId)); if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
            return Ok(await query.OrderByDescending(x => x.AppliedAt).Select(x => new { x.LeaveId, x.UserId, EmployeeName = x.User != null ? x.User.FullName : null, x.LeaveType, x.FromDate, x.ToDate, Duration = EF.Functions.DateDiffDay(x.FromDate, x.ToDate) == 0 ? "Full Day" : "Multiple Days", WorkingDays = EF.Functions.DateDiffDay(x.FromDate, x.ToDate) + 1, x.Reason, x.Status, ManagerRemarks = x.ApprovalRemarks, x.AppliedAt, DecisionDate = x.ApprovedAt }).ToListAsync());
        }

        [HttpPut("leaves/{id}/decision")]
        public async Task<IActionResult> LeaveDecision(int id, DecisionRequest request)
        {
            var leave = await _context.Leaves.Include(x => x.User).FirstOrDefaultAsync(x => x.LeaveId == id); if (leave == null) return NotFound(new { message = "Leave not found" }); if (!await IsInTeam(leave.UserId)) return Forbid(); if (leave.Status != "Pending") return Conflict(new { message = "Only pending leave requests can be reviewed" });
            var decision = request.Decision.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : request.Decision.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ? "Rejected" : null;
            if (decision == null) return BadRequest(new { message = "Decision must be Approved or Rejected" });
            var failure = await LeaveWorkflow.ReviewAsync(_context, leave, CurrentUserId(), decision, request.Comments); if (failure is not null) return StatusCode(failure.StatusCode, new { message = failure.Message });
            _context.Notifications.Add(new Notification { UserId = leave.UserId, Title = $"Leave {decision}", Message = $"Your {leave.LeaveType} request was {decision.ToLowerInvariant()}. {request.Comments}".Trim(), CreatedAt = DateTime.UtcNow, IsRead = false, Type="LeaveDecision", RelatedUrl="/employee/attendance", DeduplicationKey=$"leave:{leave.LeaveId}:{decision}:{leave.ApprovedAt:O}" }); _context.ActivityLogs.Add(new ActivityLog { UserId = leave.UserId, Action = $"Leave {decision}", CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync();
            if (leave.User != null) await _emailService.TrySendEmailAsync(leave.User.Email, $"Leave {decision}", $"<p>Your {leave.LeaveType} request was {decision.ToLowerInvariant()}.</p><p>{request.Comments}</p>"); return Ok(new { message = $"Leave {decision.ToLowerInvariant()}" });
        }

        [HttpGet("projects")]
        public async Task<IActionResult> Projects() => Ok(await ManagerProjects(CurrentUserId()).OrderByDescending(x => x.CreatedAt).Select(x => new { x.ProjectId, x.ProjectName, x.ProjectCode, x.Description, x.StartDate, x.Priority, x.Status, x.ProgressPercentage, Deadline = x.EndDate, x.IsArchived, TeamMemberCount = _context.ProjectAssignments.Count(a => a.ProjectId == x.ProjectId), TaskSummary = new { Total = _context.WorkTasks.Count(t => t.ProjectId == x.ProjectId), Pending = _context.WorkTasks.Count(t => t.ProjectId == x.ProjectId && t.Status != "Completed" && t.Status != "Cancelled"), AwaitingReview = _context.WorkTasks.Count(t => t.ProjectId == x.ProjectId && t.Status == "Ready for Review") } }).ToListAsync());

        [HttpPost("projects")]
        public IActionResult CreateProject() => StatusCode(StatusCodes.Status403Forbidden, new { message = "Projects are created and assigned by Admin" });

        [HttpPut("projects/{id}")]
        public async Task<IActionResult> UpdateProject(int id, ManagerProjectUpdateRequest request)
        {
            var project = await ManagerProjects(CurrentUserId()).FirstOrDefaultAsync(x => x.ProjectId == id && !x.IsArchived); if (project == null) return NotFound(new { message = "Project not found" });
            var allowed = new[] { "Queued", "Ongoing", "Review", "Completed", "Delayed", "At Risk", "On Hold" }; if (!allowed.Contains(request.Status, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Invalid Project status" }); if (request.ProgressPercentage is < 0 or > 100) return BadRequest(new { message = "Progress must be between 0 and 100" });
            project.Status = request.Status; project.ProgressPercentage = request.ProgressPercentage; project.UpdatedAt = DateTime.UtcNow; await _context.SaveChangesAsync(); return Ok(new { message = "Project updated" });
        }        [HttpPut("projects/{id}/archive")]
        public IActionResult ArchiveProject(int id) => StatusCode(StatusCodes.Status403Forbidden, new { message = "Managers cannot archive projects" });

        [HttpGet("projects/{id:int}/members")]
        public async Task<IActionResult> ProjectMembers(int id)
        {
            if (!await ManagerProjects(CurrentUserId()).AnyAsync(x => x.ProjectId == id && !x.IsArchived)) return NotFound(new { message = "Project not found" });
            return Ok(await _context.ProjectAssignments.AsNoTracking().Include(x => x.User).Where(x => x.ProjectId == id).Select(x => new { x.UserId, x.User!.FullName, x.User.EmployeeCode, x.User.Email }).ToListAsync());
        }
        [HttpPost("projects/{id}/assign")]
        public async Task<IActionResult> AssignProject(int id, ProjectAssignmentRequestDto request)
        {
            var employeeId = request.EmployeeId != 0 ? request.EmployeeId : request.UserId.GetValueOrDefault(); var project = await ManagerProjects(CurrentUserId()).FirstOrDefaultAsync(x => x.ProjectId == id); if (project == null) return NotFound(new { message = "Project not found" }); if (project.IsArchived || project.Status is "Completed" or "Cancelled") return Conflict(new { message = "Only active projects can receive assignments" }); if (!await IsActiveTeamMember(employeeId)) return NotFound(new { message = "Eligible active direct Employee not found" });
            if (await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == id && x.UserId == employeeId)) return Conflict(new { message = "Employee is already assigned" });
            _context.ProjectAssignments.Add(new ProjectAssignment { ProjectId = id, UserId = employeeId, AssignedAt = DateTime.UtcNow }); _context.Notifications.Add(new Notification { UserId = employeeId, Title = "Project Assigned", Message = $"You have been assigned to project {project.ProjectName}.", IsRead = false, CreatedAt = DateTime.UtcNow }); _context.ActivityLogs.Add(new ActivityLog { UserId = employeeId, Action = $"Project Assigned: {project.ProjectName}", CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == employeeId); if (user != null) { try { await _emailService.TrySendEmailAsync(user.Email, "Project Assigned", $"<p>You have been assigned to <strong>{project.ProjectName}</strong>.</p>"); } catch (InvalidOperationException) { } } return Ok(new { message = "Project assigned" });
        }

        [HttpDelete("projects/{id}/members/{employeeId}")]
        public async Task<IActionResult> RemoveProjectMember(int id, int employeeId)
        {
            var project = await ManagerProjects(CurrentUserId()).FirstOrDefaultAsync(x => x.ProjectId == id);
            if (project == null) return NotFound(new { message = "Project not found" });
            if (!await IsInTeam(employeeId)) return BadRequest(new { message = "Employee is not in your team" });
            var assignment = await _context.ProjectAssignments.FirstOrDefaultAsync(x => x.ProjectId == id && x.UserId == employeeId);
            if (assignment == null) return NotFound(new { message = "Project member assignment not found" });
            if (await _context.WorkTasks.AnyAsync(x => x.ProjectId == id && x.AssignedToId == employeeId && x.Status != "Completed" && x.Status != "Cancelled")) return Conflict(new { message = "Reassign or close active tasks before removing this project member" });
            _context.ProjectAssignments.Remove(assignment);
            _context.ActivityLogs.Add(new ActivityLog { UserId = employeeId, Action = $"Removed from project: {project.ProjectName}", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Project assignment removed; Employee account and history were preserved" });
        }
        [HttpGet("tasks")]
        public async Task<IActionResult> Tasks()
        {
            var ids = await TeamUserIds(CurrentUserId()); return Ok(await _context.WorkTasks.Include(x => x.AssignedTo).Include(x => x.Project).Where(x => ids.Contains(x.AssignedToId)).OrderBy(x => x.DueDate).Select(x => new { x.TaskId, x.AssignedToId, x.ProjectId, TaskName = x.Title, x.Title, x.Description, x.ExpectedOutput, x.ManagerNotes, x.StartDate, x.Priority, x.Status, x.ProgressPercentage, x.EstimatedHours, x.EmployeeRemarks, x.DueDate, AssignedEmployeeName = x.AssignedTo != null ? x.AssignedTo.FullName : null, ProjectName = x.Project != null ? x.Project.ProjectName : null }).ToListAsync());
        }

        [HttpPost("tasks")]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { message = "Task title is required" }); if (!await IsActiveTeamMember(dto.AssignedToId)) return NotFound(new { message = "Active direct Employee not found" }); if (!dto.ProjectId.HasValue) return BadRequest(new { message = "A managed Project is required" });
            var project = await ManagerProjects(CurrentUserId()).FirstOrDefaultAsync(x => x.ProjectId == dto.ProjectId && !x.IsArchived); if (project == null) return NotFound(new { message = "Project not found" }); if (!await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == project.ProjectId && x.UserId == dto.AssignedToId)) return Conflict(new { message = "Employee must be assigned to the Project first" }); if (dto.StartDate.HasValue && dto.DueDate.HasValue && dto.DueDate.Value.Date < dto.StartDate.Value.Date) return BadRequest(new { message = "Deadline must be on or after start date" });
            var task = new WorkTask { Title = dto.Title.Trim(), Description = dto.Description?.Trim(), ExpectedOutput = dto.ExpectedOutput?.Trim(), ManagerNotes = dto.ManagerNotes?.Trim(), StartDate = dto.StartDate, AssignedToId = dto.AssignedToId, AssignedById = CurrentUserId(), ProjectId = dto.ProjectId, Priority = dto.Priority, EstimatedHours = dto.EstimatedHours, DueDate = dto.DueDate, Status = "Assigned", CreatedAt = DateTime.UtcNow }; _context.WorkTasks.Add(task); await _context.SaveChangesAsync(); _context.Notifications.Add(new Notification { UserId = dto.AssignedToId, Title = "Task assigned", Message = $"You have been assigned task {task.Title}.", Type = "TaskAssigned", RelatedUrl = "/employee/projects", DeduplicationKey = $"task-assigned:{task.TaskId}:{dto.AssignedToId}", CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); return Ok(new { message = "Task created", task.TaskId });
        }        [HttpPut("tasks/{id:int}")]
        public async Task<IActionResult> UpdateTask(int id, CreateTaskDto dto)
        {
            var task = await _context.WorkTasks.Include(x => x.AssignedTo).FirstOrDefaultAsync(x => x.TaskId == id && x.AssignedTo!.ManagerId == CurrentUserId());
            if (task == null) return NotFound(new { message = "Task not found" });
            if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest(new { message = "Task title is required" });
            if (!await IsActiveTeamMember(dto.AssignedToId)) return NotFound(new { message = "Active direct Employee not found" });
            if (!dto.ProjectId.HasValue) return BadRequest(new { message = "A managed Project is required" });
            var project = await ManagerProjects(CurrentUserId()).FirstOrDefaultAsync(x => x.ProjectId == dto.ProjectId && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled");
            if (project == null) return NotFound(new { message = "Active Project not found" });
            if (!await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == project.ProjectId && x.UserId == dto.AssignedToId)) return Conflict(new { message = "Employee must be assigned to the Project first" });
            if (dto.StartDate.HasValue && dto.DueDate.HasValue && dto.DueDate.Value.Date < dto.StartDate.Value.Date) return BadRequest(new { message = "Deadline must be on or after start date" });
            task.Title = dto.Title.Trim(); task.Description = dto.Description?.Trim(); task.ExpectedOutput = dto.ExpectedOutput?.Trim(); task.ManagerNotes = dto.ManagerNotes?.Trim(); task.StartDate = dto.StartDate; task.AssignedToId = dto.AssignedToId; task.ProjectId = dto.ProjectId; task.Priority = dto.Priority; task.EstimatedHours = dto.EstimatedHours; task.DueDate = dto.DueDate;
            _context.Notifications.Add(new Notification { UserId = dto.AssignedToId, Title = "Task updated", Message = $"Task {task.Title} was updated by your Manager.", Type = "TaskUpdated", RelatedUrl = "/employee/projects", DeduplicationKey = $"task-updated:{task.TaskId}:{DateTime.UtcNow.Ticks}", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Task updated" });
        }
        [HttpGet("timesheets")]
        public async Task<IActionResult> Timesheets([FromQuery] string? status)
        {
            var teamIds = await TeamUserIds(CurrentUserId()); var query = _context.WeeklyTimesheets.AsNoTracking().Include(x => x.Employee).Include(x => x.ReviewedBy).Include(x => x.Lines).ThenInclude(x => x.Project).Include(x => x.Lines).ThenInclude(x => x.Task).Where(x => x.ManagerId == CurrentUserId() && teamIds.Contains(x.EmployeeId)); if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status); var rows = await query.OrderByDescending(x => x.WeekStartDate).ToListAsync();
            return Ok(rows.Select(x => new { x.Id, x.EmployeeId, employee = x.Employee!.FullName, x.WeekStartDate, x.WeekEndDate, x.TotalHours, billableHours = x.Lines.Where(l => l.BillingType == "Billable").Sum(l => l.TotalHours), nonBillableHours = x.Lines.Where(l => l.BillingType == "Non-billable").Sum(l => l.TotalHours), x.Status, x.SubmittedAt, x.ReviewedAt, reviewedBy = x.ReviewedById.HasValue ? x.ReviewedBy!.FullName : null, x.ManagerRemarks, lines = x.Lines.Select(l => new { l.Id, l.ProjectId, project = l.Project!.ProjectName, l.TaskId, task = l.TaskId.HasValue ? l.Task!.Title : null, l.WorkDescription, l.BillingType, l.MondayHours, l.TuesdayHours, l.WednesdayHours, l.ThursdayHours, l.FridayHours, l.TotalHours }) }));
        }

        [HttpPut("timesheets/{id:int}/decision")]
        public async Task<IActionResult> TimesheetDecision(int id, DecisionRequest request)
        {
            var sheet = await _context.WeeklyTimesheets.FirstOrDefaultAsync(x => x.Id == id && x.ManagerId == CurrentUserId()); if (sheet == null) return NotFound(new { message = "Timesheet not found" }); if (sheet.EmployeeId == CurrentUserId()) return Conflict(new { message = "Managers cannot review their own timesheet" }); if (sheet.Status != "Submitted") return Conflict(new { message = "Only submitted timesheets can be reviewed" });
            var decision = request.Decision.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "Approved" : request.Decision.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ? "Rejected" : request.Decision.Equals("Changes Requested", StringComparison.OrdinalIgnoreCase) ? "Changes Requested" : null; if (decision == null) return BadRequest(new { message = "Decision must be Approved, Rejected, or Changes Requested" }); if (decision != "Approved" && string.IsNullOrWhiteSpace(request.Comments)) return BadRequest(new { message = "Remarks are required for rejection or requested changes" });
            sheet.Status = decision; sheet.ManagerRemarks = request.Comments?.Trim(); sheet.ReviewedAt = DateTime.UtcNow; sheet.ReviewedById = CurrentUserId(); sheet.UpdatedAt = DateTime.UtcNow; _context.Notifications.Add(new Notification { UserId = sheet.EmployeeId, Title = $"Timesheet {decision.ToLowerInvariant()}", Message = $"Your weekly timesheet was {decision.ToLowerInvariant()}. {sheet.ManagerRemarks}".Trim(), Type = "TimesheetReview", RelatedUrl = "/employee/timesheet", DeduplicationKey = $"timesheet-review:{sheet.Id}:{decision}", CreatedAt = DateTime.UtcNow }); await _context.SaveChangesAsync(); return Ok(new { message = $"Timesheet {decision.ToLowerInvariant()}" });
        }        [HttpGet("calendar")]
        public async Task<IActionResult> Calendar([FromQuery] int? employeeId, [FromQuery] string? from, [FromQuery] string? to)
        {
            var ids = await TeamUserIds(CurrentUserId());
            if (employeeId.HasValue && !ids.Contains(employeeId.Value)) return NotFound(new { message = "Employee not found" });
            var selected = employeeId.HasValue ? new List<int> { employeeId.Value } : ids;
            var start = DateTime.TryParse(from, out var parsedStart) ? parsedStart.Date : new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var end = DateTime.TryParse(to, out var parsedEnd) ? parsedEnd.Date : start.AddMonths(1).AddDays(-1);
            var result = new List<object>();
            var tasks = await _context.WorkTasks.Where(x => selected.Contains(x.AssignedToId) && x.Status != "Cancelled" && x.DueDate >= start && x.DueDate < end.AddDays(1)).ToListAsync();
            result.AddRange(tasks.Select(x => (object)new { date = x.DueDate, title = x.Title, type = "Task Deadline", x.TaskId, x.ProjectId }));
            var projects = await ManagerProjects(CurrentUserId()).Where(x => !x.IsArchived && x.EndDate >= start && x.EndDate < end.AddDays(1)).ToListAsync();
            result.AddRange(projects.Select(x => (object)new { date = x.EndDate, title = x.ProjectName, type = "Project Deadline", x.ProjectId }));
            var holidays = await _context.CompanyHolidays.Where(x => x.IsActive && x.HolidayDate >= start && x.HolidayDate < end.AddDays(1)).ToListAsync();
            result.AddRange(holidays.Select(x => (object)new { date = x.HolidayDate, title = x.HolidayName, type = "Company Event" }));
            var events = await _context.CalendarEvents.Where(x => (x.CreatedById == CurrentUserId() || x.UserId == CurrentUserId() || selected.Contains(x.UserId)) && x.Status != "Cancelled" && x.StartTime >= start && x.StartTime < end.AddDays(1)).ToListAsync();
            result.AddRange(events.GroupBy(x => x.MeetingGroupId).Select(g => (object)new { date = g.First().StartTime, eventId = g.Min(x => x.EventId), meetingGroupId = g.Key, title = g.First().Title, type = g.First().EventType, startTime = g.First().StartTime, endTime = g.First().EndTime, description = g.First().Description, location = g.First().Location, projectId = g.First().ProjectId, createdById = g.First().CreatedById, attendeeIds = g.Select(x => x.UserId).Distinct() }));
            return Ok(result);
        }
        [HttpGet("meeting-options")]
        public async Task<IActionResult> MeetingOptions([FromQuery] int? projectId, [FromQuery] string? meetingType)
        {
            var managerId = CurrentUserId();
            if (projectId.HasValue && !await ManagerProjects(managerId).AnyAsync(x => x.ProjectId == projectId && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled"))
                return NotFound(new { message = "Active project not found" });

            var myTeam = await _context.Users.AsNoTracking()
                .Where(x => x.ManagerId == managerId && x.Role == "Employee" && x.IsActive)
                .OrderBy(x => x.FullName)
                .Select(x => new { x.UserId, x.FullName, x.Role })
                .ToListAsync();
            var projectMembers = projectId.HasValue
                ? await _context.ProjectAssignments.AsNoTracking().Where(x => x.ProjectId == projectId && x.User!.IsActive)
                    .Select(x => (object)new { x.UserId, x.User!.FullName, x.User.Role }).Distinct().ToListAsync()
                : new List<object>();
            var manager = await _context.Users.AsNoTracking().FirstAsync(x => x.UserId == managerId);
            var contacts = new List<object>();
            if (string.Equals(meetingType, "Administrative Meeting", StringComparison.OrdinalIgnoreCase))
                contacts.AddRange(await _context.Users.AsNoTracking().Where(x => x.IsActive && x.Role == "Admin").OrderBy(x => x.FullName).Select(x => (object)new { x.UserId, x.FullName, x.Role }).ToListAsync());
            if (string.Equals(meetingType, "HR Discussion", StringComparison.OrdinalIgnoreCase) && manager.AssignedHrId.HasValue)
                contacts.AddRange(await _context.Users.AsNoTracking().Where(x => x.UserId == manager.AssignedHrId && x.IsActive && x.Role == "HR").Select(x => (object)new { x.UserId, x.FullName, x.Role }).ToListAsync());
            var projects = await ManagerProjects(managerId).Where(x => !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled").OrderBy(x => x.ProjectName).Select(x => new { x.ProjectId, x.ProjectName }).ToListAsync();
            return Ok(new { groups = new object[] { new { key = "team", label = "My Team", candidates = myTeam }, new { key = "project", label = "Project Members", candidates = projectMembers }, new { key = "contacts", label = "Administrative Contacts", candidates = contacts } }, projects });
        }

        [HttpPost("meetings")]
        public async Task<IActionResult> ScheduleMeeting(MeetingRequest request)
        {
            var allowedTypes = new[] { "Team Meeting", "Project Review", "Task Discussion", "One-to-One", "Training", "Group Discussion", "HR Discussion", "Administrative Meeting", "Team Event" };
            if (string.IsNullOrWhiteSpace(request.Title)) return BadRequest(new { message = "Meeting title is required" });
            if (!allowedTypes.Contains(request.MeetingType, StringComparer.OrdinalIgnoreCase)) return BadRequest(new { message = "Select a valid meeting type" });
            if (request.EndTime <= request.StartTime) return BadRequest(new { message = "Meeting end time must be after start time" });
            if (request.InvitedUserIds.Count == 0) return BadRequest(new { message = "Select at least one attendee" });
            if (request.InvitedUserIds.Distinct().Count() != request.InvitedUserIds.Count) return Conflict(new { message = "Duplicate attendees are not allowed" });
            if (request.ProjectId.HasValue && !await ManagerProjects(CurrentUserId()).AnyAsync(x => x.ProjectId == request.ProjectId && !x.IsArchived && x.Status != "Completed" && x.Status != "Cancelled")) return NotFound(new { message = "Active project not found" });
            if (request.MeetingType.Equals("Project Review", StringComparison.OrdinalIgnoreCase) && !request.ProjectId.HasValue) return BadRequest(new { message = "Project Review requires a related project" });
            if (request.MeetingType.Equals("One-to-One", StringComparison.OrdinalIgnoreCase) && request.InvitedUserIds.Count != 1) return BadRequest(new { message = "One-to-One requires exactly one direct Employee" });
            var managerHrId = await _context.Users.Where(x => x.UserId == CurrentUserId()).Select(x => x.AssignedHrId).FirstOrDefaultAsync();
            if (request.MeetingType.Equals("HR Discussion", StringComparison.OrdinalIgnoreCase) && (!managerHrId.HasValue || !request.InvitedUserIds.Contains(managerHrId.Value))) return BadRequest(new { message = "HR Discussion requires your assigned HR contact" });
            if (request.MeetingType.Equals("Administrative Meeting", StringComparison.OrdinalIgnoreCase) && !await _context.Users.AnyAsync(x => request.InvitedUserIds.Contains(x.UserId) && x.Role == "Admin" && x.IsActive)) return BadRequest(new { message = "Administrative Meeting requires an active Admin contact" });
            foreach (var id in request.InvitedUserIds)
                if (!await IsAllowedMeetingAttendee(id, request.MeetingType, request.ProjectId)) return StatusCode(StatusCodes.Status403Forbidden, new { message = "One or more attendees are outside the permitted scope for this meeting type" });

            var zone = await CompanyZone();
            var start = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(request.StartTime, DateTimeKind.Unspecified), zone);
            var end = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(request.EndTime, DateTimeKind.Unspecified), zone);
            var group = Guid.NewGuid().ToString("N");
            foreach (var id in request.InvitedUserIds)
            {
                _context.CalendarEvents.Add(new CalendarEvent { Title = request.Title.Trim(), Description = request.Agenda?.Trim(), EventType = request.MeetingType, StartTime = start, EndTime = end, Location = string.IsNullOrWhiteSpace(request.OnlineMeetingLink) ? request.Location?.Trim() : request.OnlineMeetingLink.Trim(), ProjectId = request.ProjectId, UserId = id, CreatedById = CurrentUserId(), Status = "Scheduled", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, MeetingGroupId = group });
                _context.Notifications.Add(new Notification { UserId = id, Title = "Meeting invitation", Message = $"{request.Title} is scheduled for {request.StartTime:g}.", Type = "Meeting", RelatedUrl = "/employee/calendar", DeduplicationKey = $"meeting:{group}:{id}:created", CreatedAt = DateTime.UtcNow });
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Meeting scheduled", meetingGroupId = group, invited = request.InvitedUserIds.Count });
        }

        [HttpGet("performance")]
        public async Task<IActionResult> Performance([FromQuery] int? year, [FromQuery] string quarter = "Q1", [FromQuery] int? employeeId = null)
        {
            var selectedYear = year ?? DateTime.UtcNow.Year; var range = QuarterRange(selectedYear, quarter); if (range == null) return BadRequest(new { message = "Quarter must be Q1, Q2, Q3 or Q4" });
            if (employeeId.HasValue && !await IsInTeam(employeeId.Value)) return NotFound(new { message = "Employee not found" });
            var ids = employeeId.HasValue ? new List<int> { employeeId.Value } : await TeamUserIds(CurrentUserId()); var rows = new List<object>();
            foreach (var id in ids) rows.Add(await PerformanceForEmployee(id, selectedYear, quarter.ToUpperInvariant(), range.Value.Start, range.Value.End)); return Ok(rows);
        }

        [HttpPut("performance/{employeeId:int}")]
        public async Task<IActionResult> SavePerformance(int employeeId, PerformanceReviewRequest request)
        {
            if (!await IsInTeam(employeeId)) return NotFound(new { message = "Employee not found" }); if (QuarterRange(request.Year, request.Quarter) == null) return BadRequest(new { message = "Quarter must be Q1, Q2, Q3 or Q4" });
            var scores = new[] { request.WorkQuality, request.Timeliness, request.Collaboration, request.Communication, request.TechnicalContribution, request.ProjectContribution }; if (scores.Any(x => x < 1 || x > 5)) return BadRequest(new { message = "Evaluation ratings must be between 1 and 5" });
            var quarter = request.Quarter.ToUpperInvariant(); var review = await _context.PerformanceReviews.FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == request.Year && x.Quarter == quarter && x.ReviewerId == CurrentUserId());
            if (review == null) { review = new PerformanceReview { EmployeeId = employeeId, Year = request.Year, Quarter = quarter }; _context.PerformanceReviews.Add(review); }
            review.ReviewerId = CurrentUserId(); review.Goals=request.Goals?.Trim()??""; review.Kpis=request.Kpis?.Trim()??""; review.TrainingScore=request.TrainingScore; review.BehaviorScore=request.BehaviorScore; review.PromotionRecommendation=request.PromotionRecommendation; review.RecommendedDesignation=request.RecommendedDesignation?.Trim()??""; review.WorkQuality = request.WorkQuality; review.Timeliness = request.Timeliness; review.Collaboration = request.Collaboration; review.Communication = request.Communication; review.TechnicalContribution = request.TechnicalContribution; review.ProjectContribution = request.ProjectContribution; review.Rating = Math.Round(scores.Average(), 2); review.Strengths = request.Strengths?.Trim() ?? ""; review.ImprovementAreas = request.ImprovementAreas?.Trim() ?? ""; review.ManagerRemarks = request.FinalRemarks?.Trim() ?? ""; if(request.IsReleased&&request.PromotionRecommendation=="Recommended"&&string.IsNullOrWhiteSpace(request.RecommendedDesignation)) return BadRequest(new { message = "Recommended designation is required for a promotion recommendation" }); review.IsReleased = request.IsReleased; review.ReviewDate = DateTime.UtcNow;
            if (request.IsReleased) _context.Notifications.Add(new Notification { UserId = employeeId, Title = "Performance review released", Message = $"Your {quarter} {request.Year} review is available.", Type = "PerformanceReview", RelatedUrl = "/employee/performance", DeduplicationKey = $"performance-review:{employeeId}:{request.Year}:{quarter}", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync(); return Ok(new { message = request.IsReleased ? "Performance review released" : "Performance review saved" });
        }        [HttpPost("team-notices")]
        public async Task<IActionResult> TeamNotice(TeamNoticeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "Title and message are required" });
            var permitted = await ActiveTeamUserIds(CurrentUserId()); var ids = request.EmployeeIds.Count == 0 ? permitted : request.EmployeeIds.Distinct().ToList(); if (ids.Any(x => !permitted.Contains(x))) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Team notices may target direct Employees only" });
            var group = Guid.NewGuid().ToString("N"); foreach (var id in ids) { _context.CalendarEvents.Add(new CalendarEvent { Title = request.Title.Trim(), Description = request.Message.Trim(), EventType = string.IsNullOrWhiteSpace(request.EventType) ? "Team Notice" : request.EventType, StartTime = request.Date.Date, EndTime = request.Date.Date.AddMinutes(1), UserId = id, CreatedById = CurrentUserId(), MeetingGroupId = group, CreatedAt = DateTime.UtcNow }); _context.Notifications.Add(new Notification { UserId = id, Title = request.Title.Trim(), Message = request.Message.Trim(), Type = "TeamNotice", RelatedUrl = "/employee/calendar", DeduplicationKey = $"team-notice:{group}:{id}", CreatedAt = DateTime.UtcNow }); }
            await _context.SaveChangesAsync(); return Ok(new { message = "Team notice created", recipients = ids.Count });
        }
        [HttpPost("announcements")]
        public async Task<IActionResult> CreateAnnouncement(AnnouncementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message)) return BadRequest(new { message = "Title and message are required" }); var ids = await TeamUserIds(CurrentUserId()); foreach (var id in ids) _context.Notifications.Add(new Notification { UserId = id, Title = $"Announcement: {request.Title}", Message = request.Message, IsRead = false, CreatedAt = request.ScheduledAt ?? DateTime.UtcNow }); await _context.SaveChangesAsync(); return Ok(new { message = "Announcement sent", recipients = ids.Count });
        }

        [HttpGet("announcements")]
        public async Task<IActionResult> Announcements() => Ok(await TeamAnnouncements(CurrentUserId()));
        [HttpGet("reports/summary")]
        public async Task<IActionResult> ReportSummary(int? employeeId = null, int? projectId = null, string? status = null, DateTime? from = null, DateTime? to = null, int? year = null, string? quarter = null)
        {
            var ids = await TeamUserIds(CurrentUserId());
            if (employeeId.HasValue && !ids.Contains(employeeId.Value)) return NotFound(new { message = "Employee not found" });
            if (projectId.HasValue && !await ManagerProjects(CurrentUserId()).AnyAsync(x => x.ProjectId == projectId.Value)) return NotFound(new { message = "Project not found" });
            var types = new[] { "project", "task", "overdue", "performance", "attendance", "leave", "timesheet", "workload" };
            var counts = new Dictionary<string, int>();
            foreach (var type in types) counts[type] = (await BuildReportRows(type, employeeId, projectId, status, from, to, year, quarter)).Count;
            var trendStart=(from??DateTime.UtcNow.Date.AddMonths(-11)).Date;var trendEnd=(to??DateTime.UtcNow.Date).Date.AddDays(1);var attendance=await _context.Attendances.Where(x=>ids.Contains(x.UserId)&&x.AttendanceDate>=trendStart&&x.AttendanceDate<trendEnd).ToListAsync();var tasks=await _context.WorkTasks.Where(x=>ids.Contains(x.AssignedToId)&&x.CreatedAt>=trendStart&&x.CreatedAt<trendEnd).ToListAsync();var trends=Enumerable.Range(0,12).Select(i=>trendStart.AddMonths(i)).Where(m=>m<trendEnd).Select(m=>new{month=m.ToString("yyyy-MM"),attendance=attendance.Count(x=>x.AttendanceDate.Year==m.Year&&x.AttendanceDate.Month==m.Month),tasks=tasks.Count(x=>x.CreatedAt.Year==m.Year&&x.CreatedAt.Month==m.Month),completed=tasks.Count(x=>x.CompletedAt.HasValue&&x.CompletedAt.Value.Year==m.Year&&x.CompletedAt.Value.Month==m.Month)}); return Ok(new { generatedAt = DateTime.UtcNow, counts, trends });
        }
        [HttpGet("reports/{type}/export")]
        public async Task<IActionResult> Export(string type, string format = "csv", int? employeeId = null, int? projectId = null, string? status = null, DateTime? from = null, DateTime? to = null, int? year = null, string? quarter = null)
        {
            var rows = await BuildReportRows(type, employeeId, projectId, status, from, to, year, quarter);
            if (!rows.Any()) rows.Add(new Dictionary<string, object?> { ["Message"] = "No records found for the selected filters" });
            var safeType = new string(type.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd");
            if (format.Equals("csv", StringComparison.OrdinalIgnoreCase)) return File(Encoding.UTF8.GetBytes(ToCsv(rows)), "text/csv", $"manager-{safeType}-{stamp}.csv");
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase)) return File(SimplePdf(rows), "application/pdf", $"manager-{safeType}-{stamp}.pdf");
            using var workbook = new XLWorkbook(); var ws = workbook.Worksheets.Add(safeType.Length > 31 ? safeType[..31] : safeType); var headers = rows.First().Keys.ToList(); for (var i = 0; i < headers.Count; i++) ws.Cell(1, i + 1).Value = headers[i]; for (var r = 0; r < rows.Count; r++) for (var c = 0; c < headers.Count; c++) ws.Cell(r + 2, c + 1).Value = CsvSafe(rows[r].GetValueOrDefault(headers[c])); ws.Columns().AdjustToContents(); using var stream = new MemoryStream(); workbook.SaveAs(stream); return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"manager-{safeType}-{stamp}.xlsx");
        }

        private async Task<List<Dictionary<string, object?>>> BuildReportRows(string type, int? employeeId, int? projectId, string? status, DateTime? from, DateTime? to, int? year, string? quarter)
        {
            var ids = await TeamUserIds(CurrentUserId());
            if (employeeId.HasValue && !ids.Contains(employeeId.Value)) return new();
            var selected = employeeId.HasValue ? new List<int> { employeeId.Value } : ids;
            var start = from?.Date ?? DateTime.MinValue;
            var endExclusive = to?.Date.AddDays(1) ?? DateTime.MaxValue;
            type = type.ToLowerInvariant();
            if (type == "attendance")
            {
                var q = _context.Attendances.AsNoTracking().Include(x => x.User).Where(x => selected.Contains(x.UserId) && x.AttendanceDate >= start && x.AttendanceDate < endExclusive);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.AttendanceStatus == status || x.Status == status);
                return (await q.ToListAsync()).Select(x => new Dictionary<string, object?> { ["Employee"] = x.User?.FullName, ["Date"] = x.AttendanceDate.ToString("yyyy-MM-dd"), ["Status"] = x.AttendanceStatus ?? x.Status, ["Hours"] = x.EffectiveHours, ["LateMinutes"] = x.LateMinutes }).ToList();
            }
            if (type == "leave")
            {
                var q = _context.Leaves.AsNoTracking().Include(x => x.User).Where(x => selected.Contains(x.UserId) && x.FromDate < endExclusive && x.ToDate >= start);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return (await q.ToListAsync()).Select(x => new Dictionary<string, object?> { ["Employee"] = x.User?.FullName, ["Type"] = x.LeaveType, ["From"] = x.FromDate.ToString("yyyy-MM-dd"), ["To"] = x.ToDate.ToString("yyyy-MM-dd"), ["Status"] = x.Status }).ToList();
            }
            if (type == "timesheet")
            {
                var q = _context.WeeklyTimesheets.AsNoTracking().Include(x => x.Employee).Where(x => selected.Contains(x.EmployeeId) && x.WeekStartDate >= start && x.WeekStartDate < endExclusive);
                if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return (await q.ToListAsync()).Select(x => new Dictionary<string, object?> { ["Employee"] = x.Employee?.FullName, ["Week"] = $"{x.WeekStartDate:yyyy-MM-dd} to {x.WeekEndDate:yyyy-MM-dd}", ["Hours"] = x.TotalHours, ["Status"] = x.Status, ["Submitted"] = x.SubmittedAt }).ToList();
            }
            if (type == "project")
            {
                var q = ManagerProjects(CurrentUserId()); if (projectId.HasValue) q = q.Where(x => x.ProjectId == projectId); if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
                return (await q.ToListAsync()).Select(x => new Dictionary<string, object?> { ["Project"] = x.ProjectName, ["Code"] = x.ProjectCode, ["Status"] = x.Status, ["Progress"] = $"{x.ProgressPercentage}%", ["Deadline"] = x.EndDate.ToString("yyyy-MM-dd") }).ToList();
            }
            var tasks = _context.WorkTasks.AsNoTracking().Include(x => x.AssignedTo).Include(x => x.Project).Where(x => selected.Contains(x.AssignedToId));
            if (projectId.HasValue) tasks = tasks.Where(x => x.ProjectId == projectId); if (!string.IsNullOrWhiteSpace(status)) tasks = tasks.Where(x => x.Status == status); if (from.HasValue) tasks = tasks.Where(x => x.CreatedAt >= start); if (to.HasValue) tasks = tasks.Where(x => x.CreatedAt < endExclusive);
            if (type == "task" || type == "overdue")
            {
                if (type == "overdue") tasks = tasks.Where(x => x.DueDate < DateTime.UtcNow && x.Status != "Completed" && x.Status != "Cancelled");
                return (await tasks.ToListAsync()).Select(x => new Dictionary<string, object?> { ["Employee"] = x.AssignedTo?.FullName, ["Project"] = x.Project?.ProjectName, ["Task"] = x.Title, ["Priority"] = x.Priority, ["Due"] = x.DueDate?.ToString("yyyy-MM-dd"), ["Status"] = x.Status, ["Progress"] = $"{x.ProgressPercentage}%" }).ToList();
            }
            if (type == "workload")
                return (await tasks.ToListAsync()).GroupBy(x => x.AssignedTo?.FullName ?? "Unknown").Select(g => new Dictionary<string, object?> { ["Employee"] = g.Key, ["ActiveTasks"] = g.Count(x => x.Status != "Completed" && x.Status != "Cancelled"), ["OverdueTasks"] = g.Count(x => x.DueDate < DateTime.UtcNow && x.Status != "Completed" && x.Status != "Cancelled"), ["ReadyForReview"] = g.Count(x => x.Status == "Ready for Review") }).ToList();
            if (type == "performance")
            {
                var q = _context.PerformanceReviews.AsNoTracking().Where(x => x.ReviewerId == CurrentUserId() && selected.Contains(x.EmployeeId)); if (year.HasValue) q = q.Where(x => x.Year == year); if (!string.IsNullOrWhiteSpace(quarter)) q = q.Where(x => x.Quarter == quarter);
                var reviews = await q.ToListAsync(); var names = await _context.Users.AsNoTracking().Where(x => selected.Contains(x.UserId)).ToDictionaryAsync(x => x.UserId, x => x.FullName);
                return reviews.Select(x => new Dictionary<string, object?> { ["Employee"] = names.GetValueOrDefault(x.EmployeeId), ["Year"] = x.Year, ["Quarter"] = x.Quarter, ["Rating"] = x.Rating, ["Status"] = x.IsReleased ? "Released" : "Draft" }).ToList();
            }            return new();
        }

        private async Task<object> PerformanceForEmployee(int employeeId, int year, string quarter, DateTime start, DateTime end)
        {
            var employee = await _context.Users.AsNoTracking().FirstAsync(x => x.UserId == employeeId);
            var localToday = (await CompanyLocalNow()).Date;
            var offboardingEnd = await _context.OffboardingRecords.AsNoTracking()
                .Where(x => x.UserId == employeeId && x.Status == "Completed")
                .OrderByDescending(x => x.LastWorkingDate)
                .Select(x => (DateTime?)x.LastWorkingDate)
                .FirstOrDefaultAsync();
            var employmentStart = employee.DateOfJoining?.Date ?? start;
            var employmentEndExclusive = offboardingEnd.HasValue ? offboardingEnd.Value.Date.AddDays(1) : end;
            var effectiveStart = employmentStart > start ? employmentStart : start;
            var effectiveEnd = new[] { end, localToday.AddDays(1), employmentEndExclusive }.Min();
            var expectedWeeks = CompletedWeekStarts(effectiveStart, effectiveEnd, localToday);
            var approvedLeaves = await _context.Leaves.AsNoTracking().Where(x => x.UserId == employeeId && x.Status == "Approved" && x.FromDate < effectiveEnd && x.ToDate >= effectiveStart).ToListAsync();
            expectedWeeks = expectedWeeks.Where(monday => !approvedLeaves.Any(l => l.FromDate.Date <= monday && l.ToDate.Date >= monday.AddDays(4))).ToList();

            var attendance = await _context.Attendances.AsNoTracking().Where(x => x.UserId == employeeId && x.AttendanceDate >= start && x.AttendanceDate < end).ToListAsync();
            var sheets = await _context.WeeklyTimesheets.AsNoTracking().Include(x => x.Lines).Where(x => x.EmployeeId == employeeId && x.WeekStartDate >= start && x.WeekStartDate < end).ToListAsync();
            var submittedWeeks = sheets.Where(x => x.Status != "Draft").Select(x => x.WeekStartDate.Date).Distinct().ToHashSet();
            var missingTimesheets = expectedWeeks.Count(x => !submittedWeeks.Contains(x));
            var tasks = await _context.WorkTasks.AsNoTracking().Where(x => x.AssignedToId == employeeId && ((x.DueDate >= start && x.DueDate < end) || (x.CreatedAt >= start && x.CreatedAt < end))).ToListAsync();
            var projects = await _context.ProjectAssignments.AsNoTracking().Include(x => x.Project).Where(x => x.UserId == employeeId).ToListAsync();
            var review = await _context.PerformanceReviews.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeId == employeeId && x.Year == year && x.Quarter == quarter && x.ReviewerId == CurrentUserId());
            var completed = tasks.Count(x => x.Status == "Completed");
            var onTime = tasks.Count(x => x.Status == "Completed" && (!x.DueDate.HasValue || x.CompletedAt <= x.DueDate));
            return new
            {
                employeeId, employeeName = employee.FullName, year, quarter,
                workHours = new
                {
                    averageEffectiveHours = attendance.Count == 0 ? 0 : Math.Round(attendance.Average(x => x.EffectiveHours > 0 ? x.EffectiveHours : x.TotalHours), 2),
                    totalTimesheetHours = sheets.Sum(x => x.TotalHours),
                    billableHours = sheets.SelectMany(x => x.Lines).Where(x => x.BillingType == "Billable").Sum(x => x.TotalHours),
                    nonBillableHours = sheets.SelectMany(x => x.Lines).Where(x => x.BillingType == "Non-billable").Sum(x => x.TotalHours),
                    missingTimesheets,
                    expectedCompletedWeeks = expectedWeeks.Count,
                    missingTimesheetFormula = "Completed Monday-Friday weeks in the selected quarter up to company-local today, excluding the current incomplete week, pre-joining weeks, post-offboarding weeks, and approved full-week leave; minus submitted weeks.",
                    missingCheckoutCount = attendance.Count(x => x.CheckInTime != null && x.CheckOutTime == null)
                },
                taskPerformance = new { assigned = tasks.Count, completed, pending = tasks.Count(x => x.Status is "Assigned" or "Pending" or "In Progress" or "Blocked"), overdue = tasks.Count(x => x.DueDate < DateTime.UtcNow && x.Status != "Completed" && x.Status != "Cancelled"), reopened = tasks.Count(x => x.Status == "Reopened"), readyForReview = tasks.Count(x => x.Status == "Ready for Review"), onTimeCompletionRate = completed == 0 ? 0 : Math.Round(onTime * 100d / completed, 2), averageProgress = tasks.Count == 0 ? 0 : Math.Round(tasks.Average(x => x.ProgressPercentage), 2), feedbackSummary = tasks.Where(x => !string.IsNullOrWhiteSpace(x.ManagerFeedback)).Select(x => x.ManagerFeedback).Take(5) },
                projectPerformance = new { assignedProjects = projects.Count, activeProjects = projects.Count(x => x.Project != null && !x.Project.IsArchived && x.Project.Status != "Completed"), completedContributions = projects.Count(x => x.Project != null && x.Project.Status == "Completed"), contributionProgress = projects.Count == 0 ? 0 : Math.Round(projects.Average(x => x.Project?.ProgressPercentage ?? 0), 2), projectDeadlinesMet = projects.Count(x => x.Project != null && x.Project.Status == "Completed" && x.Project.EndDate <= localToday), projectTimesheetHours = sheets.Sum(x => x.TotalHours), qualityFeedback = review?.Strengths, deliveryFeedback = review?.ManagerRemarks },
                evaluation = review == null ? null : new { review.PerformanceReviewId, review.WorkQuality, review.Timeliness, review.Collaboration, review.Communication, review.TechnicalContribution, review.ProjectContribution, review.Strengths, review.ImprovementAreas, finalRemarks = review.ManagerRemarks, review.IsReleased, review.ReviewDate }
            };
        }

        private static List<DateTime> CompletedWeekStarts(DateTime start, DateTime endExclusive, DateTime localToday)
        {
            var first = start.Date.AddDays(-(((int)start.DayOfWeek + 6) % 7));
            if (first < start.Date) first = first.AddDays(7);
            var weeks = new List<DateTime>();
            for (var week = first; week < endExclusive.Date; week = week.AddDays(7))
                if (week.AddDays(4) < localToday && week.AddDays(5) <= endExclusive.Date) weeks.Add(week);
            return weeks;
        }

        private async Task<TimeZoneInfo> CompanyZone()
        {
            var name = await _context.SystemSettings.Where(x => x.SettingKey == "CompanyTimezone").Select(x => x.SettingValue).FirstOrDefaultAsync() ?? "India Standard Time";
            try { return TimeZoneInfo.FindSystemTimeZoneById(name); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        }

        private async Task<DateTime> CompanyLocalNow() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, await CompanyZone());

        private static (DateTime Start, DateTime End)? QuarterRange(int year, string quarter) => quarter.ToUpperInvariant() switch { "Q1" => (new(year, 1, 1), new(year, 4, 1)), "Q2" => (new(year, 4, 1), new(year, 7, 1)), "Q3" => (new(year, 7, 1), new(year, 10, 1)), "Q4" => (new(year, 10, 1), new(year + 1, 1, 1)), _ => null };        private IQueryable<Project> ManagerProjects(int managerId) => _context.Projects.Where(x => x.ManagerId == managerId);
        private async Task<List<int>> TeamUserIds(int managerId) => await _context.Users.Where(x => x.ManagerId == managerId && x.Role == "Employee").Select(x => x.UserId).ToListAsync();
        private async Task<List<int>> ActiveTeamUserIds(int managerId) => await _context.Users.Where(x => x.ManagerId == managerId && x.Role == "Employee" && x.IsActive).Select(x => x.UserId).ToListAsync();
        private async Task<bool> IsInTeam(int employeeId) => await _context.Users.AnyAsync(x => x.UserId == employeeId && x.ManagerId == CurrentUserId() && x.Role == "Employee");
        private async Task<bool> IsActiveTeamMember(int employeeId) => await _context.Users.AnyAsync(x => x.UserId == employeeId && x.ManagerId == CurrentUserId() && x.Role == "Employee" && x.IsActive);
        private async Task<bool> IsAllowedMeetingAttendee(int userId, string meetingType, int? projectId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive);
            if (user == null || user.UserId == CurrentUserId()) return false;
            if (user.Role == "Admin") return meetingType.Equals("Administrative Meeting", StringComparison.OrdinalIgnoreCase);
            if (user.Role == "HR")
            {
                if (!meetingType.Equals("HR Discussion", StringComparison.OrdinalIgnoreCase)) return false;
                var managerHrId = await _context.Users.Where(x => x.UserId == CurrentUserId()).Select(x => x.AssignedHrId).FirstOrDefaultAsync();
                return managerHrId == user.UserId;
            }
            if (user.Role != "Employee") return false;
            var direct = user.ManagerId == CurrentUserId();
            if (meetingType is "Team Meeting" or "One-to-One" or "HR Discussion") return direct;
            var projectMember = projectId.HasValue && await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == projectId && x.UserId == userId && x.Project!.ManagerId == CurrentUserId() && !x.Project.IsArchived);
            return direct || projectMember;
        }
        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private async Task<List<object>> TeamAnnouncements(int managerId) { var ids = await TeamUserIds(managerId); return await _context.Notifications.Where(x => ids.Contains(x.UserId) && x.Title.StartsWith("Announcement:")).OrderByDescending(x => x.CreatedAt).Take(25).Select(x => new { x.Id, x.Title, x.Message, x.CreatedAt, x.IsRead, x.UserId }).Cast<object>().ToListAsync(); }
        private static string CsvSafe(object? value)
        {
            var text = value?.ToString() ?? "";
            return text.Length > 0 && "=+-@".Contains(text[0]) ? "'" + text : text;
        }

        private static byte[] SimplePdf(List<Dictionary<string, object?>> rows)
        {
            var lines=new List<string>{"Employee Monitoring System Manager Report",DateTime.UtcNow.ToString("u")};lines.AddRange(rows.Take(100).Select(x=>string.Join(" | ",x.Select(v=>$"{v.Key}: {CsvSafe(v.Value)}"))));var text=string.Join("\\n",lines).Replace("(","[").Replace(")","]");var body=$"BT /F1 9 Tf 40 800 Td ({text.Replace("\\n",") Tj 0 -12 Td (")}) Tj ET";var objs=new[]{"<< /Type /Catalog /Pages 2 0 R >>","<< /Type /Pages /Kids [3 0 R] /Count 1 >>","<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 842] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",$"<< /Length {Encoding.ASCII.GetByteCount(body)} >>\nstream\n{body}\nendstream","<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"};var b=new StringBuilder("%PDF-1.4\n");var offsets=new List<int>();for(var i=0;i<objs.Length;i++){offsets.Add(Encoding.ASCII.GetByteCount(b.ToString()));b.Append($"{i+1} 0 obj\n{objs[i]}\nendobj\n");}var start=Encoding.ASCII.GetByteCount(b.ToString());b.Append($"xref\n0 {objs.Length+1}\n0000000000 65535 f \n");foreach(var o in offsets)b.Append($"{o:0000000000} 00000 n \n");b.Append($"trailer << /Size {objs.Length+1} /Root 1 0 R >>\nstartxref\n{start}\n%%EOF");return Encoding.ASCII.GetBytes(b.ToString());
        }
        private static string ToCsv(List<Dictionary<string, object?>> rows)
        {
            var headers = rows.First().Keys.ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", headers.Select(h => $"\"{CsvSafe(row.GetValueOrDefault(h)).Replace("\"", "\"\"")}\"")));
            return sb.ToString();
        }
    }
    public class ManagerProjectUpdateRequest { public string Status { get; set; } = "Ongoing"; public int ProgressPercentage { get; set; } }
    public class DecisionRequest { public string Decision { get; set; } = "Approved"; public string? Comments { get; set; } }
    public class MeetingRequest { public string MeetingType { get; set; } = "Team Meeting"; public string Title { get; set; } = string.Empty; public List<int> InvitedUserIds { get; set; } = new(); public string? Agenda { get; set; } public string? Location { get; set; } public string? OnlineMeetingLink { get; set; } public string? Notes { get; set; } public DateTime StartTime { get; set; } public DateTime EndTime { get; set; } public int? ProjectId { get; set; } }
    public class TeamNoticeRequest { public string Title { get; set; } = ""; public string Message { get; set; } = ""; public DateTime Date { get; set; } public string? EventType { get; set; } public List<int> EmployeeIds { get; set; } = new(); }
    public class PerformanceReviewRequest { public string? Goals { get; set; } public string? Kpis { get; set; } public decimal TrainingScore { get; set; } public decimal BehaviorScore { get; set; } public string PromotionRecommendation { get; set; } = "Not Recommended"; public string? RecommendedDesignation { get; set; } public int Year { get; set; } public string Quarter { get; set; } = "Q1"; public decimal WorkQuality { get; set; } public decimal Timeliness { get; set; } public decimal Collaboration { get; set; } public decimal Communication { get; set; } public decimal TechnicalContribution { get; set; } public decimal ProjectContribution { get; set; } public string? Strengths { get; set; } public string? ImprovementAreas { get; set; } public string? FinalRemarks { get; set; } public bool IsReleased { get; set; } }
    public class AnnouncementRequest { public string Title { get; set; } = string.Empty; public string Message { get; set; } = string.Empty; public DateTime? ScheduledAt { get; set; } }
}

