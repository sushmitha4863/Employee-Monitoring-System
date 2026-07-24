using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Employee,Manager,HR")]
    public class TimesheetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TimesheetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("my")]
        public async Task<IActionResult> MyTimesheets()
        {
            var userId = CurrentUserId();
            var rows = await _context.TimesheetEntries
                .Include(x => x.Project)
                .Include(x => x.Task)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.WorkDate)
                .ThenByDescending(x => x.SubmittedAt)
                .Select(x => new
                {
                    x.TimesheetEntryId,
                    x.ProjectId,
                    ProjectName = x.Project != null ? x.Project.ProjectName : null,
                    x.TaskId,
                    TaskName = x.Task != null ? x.Task.Title : null,
                    x.WorkDate,
                    StartTime = x.StartTime.ToString(@"hh\:mm"),
                    EndTime = x.EndTime.ToString(@"hh\:mm"),
                    x.Hours,
                    x.IsBillable,
                    x.WorkDescription,
                    x.DailyNotes,
                    x.Status,
                    x.ManagerComments,
                    x.SubmittedAt
                })
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var userId = CurrentUserId();
            var weekStart = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek + 1);
            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var rows = await _context.TimesheetEntries.Where(x => x.UserId == userId).ToListAsync();
            return Ok(new
            {
                totalHours = Math.Round(rows.Sum(x => x.Hours), 2),
                weeklyHours = Math.Round(rows.Where(x => x.WorkDate.Date >= weekStart).Sum(x => x.Hours), 2),
                monthlyHours = Math.Round(rows.Where(x => x.WorkDate.Date >= monthStart).Sum(x => x.Hours), 2),
                billableHours = Math.Round(rows.Where(x => x.IsBillable).Sum(x => x.Hours), 2),
                submitted = rows.Count(x => x.Status == "Submitted"),
                approved = rows.Count(x => x.Status == "Approved"),
                rejected = rows.Count(x => x.Status == "Rejected")
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create(TimesheetEntryDto dto)
        {
            var userId = CurrentUserId();
            if (dto.WorkDate.Date > DateTime.Today) return BadRequest(new { message = "Timesheet date cannot be in the future" });
            if (!TimeSpan.TryParse(dto.StartTime, out var start) || !TimeSpan.TryParse(dto.EndTime, out var end)) return BadRequest(new { message = "Valid start and end times are required" });
            if (end <= start) return BadRequest(new { message = "End time must be after start time" });
            if (string.IsNullOrWhiteSpace(dto.WorkDescription) || dto.WorkDescription.Trim().Length < 10) return BadRequest(new { message = "Work description must be at least 10 characters" });

            var hours = Math.Round((end - start).TotalHours, 2);
            var attendance = await _context.Attendances.FirstOrDefaultAsync(x => x.UserId == userId && x.AttendanceDate.Date == dto.WorkDate.Date);
            var allowedHours = attendance?.EffectiveHours > 0 ? attendance.EffectiveHours : attendance?.TotalHours ?? 0;
            if (allowedHours > 0)
            {
                var alreadyLogged = await _context.TimesheetEntries.Where(x => x.UserId == userId && x.WorkDate.Date == dto.WorkDate.Date).SumAsync(x => x.Hours);
                if (alreadyLogged + hours > allowedHours + 0.05) return BadRequest(new { message = "Timesheet hours cannot exceed attendance working hours" });
            }

            if (dto.ProjectId.HasValue)
            {
                var assigned = await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == dto.ProjectId.Value && x.UserId == userId);
                if (!assigned) return BadRequest(new { message = "You can log time only against assigned projects" });
            }

            if (dto.TaskId.HasValue)
            {
                var task = await _context.WorkTasks.FirstOrDefaultAsync(x => x.TaskId == dto.TaskId.Value && x.AssignedToId == userId);
                if (task == null) return BadRequest(new { message = "You can log time only against assigned tasks" });
                if (dto.ProjectId.HasValue && task.ProjectId.HasValue && task.ProjectId != dto.ProjectId) return BadRequest(new { message = "Selected task does not belong to the selected project" });
            }

            var duplicate = await _context.TimesheetEntries.AnyAsync(x =>
                x.UserId == userId &&
                x.WorkDate.Date == dto.WorkDate.Date &&
                x.ProjectId == dto.ProjectId &&
                x.TaskId == dto.TaskId &&
                x.StartTime == start &&
                x.EndTime == end);
            if (duplicate) return BadRequest(new { message = "Duplicate timesheet entry already exists" });

            var entry = new TimesheetEntry
            {
                UserId = userId,
                ProjectId = dto.ProjectId,
                TaskId = dto.TaskId,
                WorkDate = dto.WorkDate.Date,
                StartTime = start,
                EndTime = end,
                Hours = hours,
                IsBillable = dto.IsBillable,
                WorkDescription = dto.WorkDescription.Trim(),
                DailyNotes = dto.DailyNotes,
                Status = "Submitted",
                SubmittedAt = DateTime.UtcNow
            };

            _context.TimesheetEntries.Add(entry);
            _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = $"Timesheet Submitted: {entry.WorkDate:dd MMM yyyy}", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Timesheet submitted", entry.TimesheetEntryId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TimesheetEntryDto dto)
        {
            var userId = CurrentUserId();
            var entry = await _context.TimesheetEntries.FirstOrDefaultAsync(x => x.TimesheetEntryId == id && x.UserId == userId);
            if (entry == null) return NotFound(new { message = "Timesheet entry not found" });
            if (entry.Status == "Approved") return BadRequest(new { message = "Approved timesheets cannot be edited" });

            if (!TimeSpan.TryParse(dto.StartTime, out var start) || !TimeSpan.TryParse(dto.EndTime, out var end)) return BadRequest(new { message = "Valid start and end times are required" });
            if (end <= start) return BadRequest(new { message = "End time must be after start time" });

            entry.ProjectId = dto.ProjectId;
            entry.TaskId = dto.TaskId;
            entry.WorkDate = dto.WorkDate.Date;
            entry.StartTime = start;
            entry.EndTime = end;
            entry.Hours = Math.Round((end - start).TotalHours, 2);
            entry.IsBillable = dto.IsBillable;
            entry.WorkDescription = dto.WorkDescription.Trim();
            entry.DailyNotes = dto.DailyNotes;
            entry.Status = "Submitted";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Timesheet updated" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = CurrentUserId();
            var entry = await _context.TimesheetEntries.FirstOrDefaultAsync(x => x.TimesheetEntryId == id && x.UserId == userId);
            if (entry == null) return NotFound(new { message = "Timesheet entry not found" });
            if (entry.Status == "Approved") return BadRequest(new { message = "Approved timesheets cannot be deleted" });
            _context.TimesheetEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Timesheet deleted" });
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
