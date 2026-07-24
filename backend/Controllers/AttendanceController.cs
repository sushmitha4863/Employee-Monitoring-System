using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("check-in"), EnableRateLimiting("attendance-action")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> CheckIn()
        {
            var userId = CurrentUserId();
            var today = DateTime.Today;
            var now = DateTime.Now;
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);
            if (user == null || !user.IsActive) return Forbid();

            if (await _context.Attendances.AnyAsync(x => x.UserId == userId && x.AttendanceDate.Date == today))
            {
                return Conflict(new { message = "Already checked in today" });
            }

            var isHoliday = await _context.CompanyHolidays.AnyAsync(x => x.IsActive && x.HolidayDate.Date == today);
            if (isHoliday && !await SettingEnabled("Attendance.AllowHolidayCheckIn"))
            {
                return BadRequest(new { message = "Check-in on holidays is disabled by company policy" });
            }

            var isWeekend = today.DayOfWeek == DayOfWeek.Saturday || today.DayOfWeek == DayOfWeek.Sunday;
            if (isWeekend && !await SettingEnabled("Attendance.AllowWeekendCheckIn"))
            {
                return BadRequest(new { message = "Check-in on weekends is disabled by company policy" });
            }

            var status = ResolveStatus(now.TimeOfDay);
            var lateMinutes = now.TimeOfDay > new TimeSpan(9, 40, 0) ? (int)(now.TimeOfDay - new TimeSpan(9, 40, 0)).TotalMinutes : 0;

            var attendance = new Attendance
            {
                UserId = userId,
                AttendanceDate = today,
                CheckInTime = now,
                LateMinutes = lateMinutes,
                Status = status,
                AttendanceStatus = status
            };

            _context.Attendances.Add(attendance);
            _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = "Check In", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Check-in successful", attendanceId = attendance.AttendanceId, attendanceDate = attendance.AttendanceDate, checkInTime = attendance.CheckInTime, checkOutTime = attendance.CheckOutTime, workingHours = attendance.TotalHours, breakHours = Math.Round(attendance.BreakMinutes / 60.0, 2), effectiveHours = attendance.EffectiveHours, overtimeHours = attendance.OvertimeHours, lateMinutes = attendance.LateMinutes, attendanceStatus = attendance.AttendanceStatus });
        }

        [HttpPost("start-break"), EnableRateLimiting("attendance-action")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> StartBreak()
        {
            var attendance = await TodayAttendance();
            if (attendance == null) return BadRequest(new { message = "Check-in not found" });
            if (attendance.CheckOutTime.HasValue) return BadRequest(new { message = "Cannot start break after check-out" });
            if (attendance.BreakStartTime.HasValue && !attendance.BreakEndTime.HasValue) return BadRequest(new { message = "Break already started" });

            attendance.BreakStartTime = await CompanyNow();
            attendance.BreakEndTime = null;
            _context.ActivityLogs.Add(new ActivityLog { UserId = attendance.UserId, Action = "Break Started", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Break started", breakStartTime = attendance.BreakStartTime, workingHours = CalculateWorkingHours(attendance), breakMinutes = attendance.BreakMinutes, breakHours = Math.Round(attendance.BreakMinutes / 60.0, 2), effectiveHours = CalculateEffectiveHours(attendance), overtimeHours = Math.Max(0, Math.Round(CalculateEffectiveHours(attendance) - 9, 2)) });
        }

        [HttpPost("end-break"), EnableRateLimiting("attendance-action")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> EndBreak()
        {
            var attendance = await TodayAttendance();
            if (attendance == null) return BadRequest(new { message = "Check-in not found" });
            if (!attendance.BreakStartTime.HasValue || attendance.BreakEndTime.HasValue) return BadRequest(new { message = "No active break found" });

            attendance.BreakEndTime = await CompanyNow();
            attendance.BreakMinutes += Math.Max(0, (int)(attendance.BreakEndTime.Value - attendance.BreakStartTime.Value).TotalMinutes);
            attendance.EffectiveHours = CalculateEffectiveHours(attendance);
            var breakWorkingHours = CalculateWorkingHours(attendance);
            attendance.EffectiveHours = CalculateEffectiveHours(attendance);
            _context.ActivityLogs.Add(new ActivityLog { UserId = attendance.UserId, Action = "Break Ended", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Break ended", workingHours = breakWorkingHours, breakMinutes = attendance.BreakMinutes, breakHours = Math.Round(attendance.BreakMinutes / 60.0, 2), effectiveHours = attendance.EffectiveHours, overtimeHours = Math.Max(0, Math.Round(attendance.EffectiveHours - 9, 2)) });
        }

        [HttpPost("check-out"), EnableRateLimiting("attendance-action")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> CheckOut([FromBody] string? workSummary)
        {
            var attendance = await TodayAttendance();
            if (attendance == null) return BadRequest(new { message = "Check-in not found" });
            if (attendance.CheckOutTime != null) return BadRequest(new { message = "Already checked out" });
            if (attendance.BreakStartTime.HasValue && !attendance.BreakEndTime.HasValue) return BadRequest(new { message = "End active break before check-out" });
            if (string.IsNullOrWhiteSpace(workSummary)) return BadRequest(new { message = "Work summary is required" });
            if (workSummary.Trim().Length < 20) return BadRequest(new { message = "Minimum 20 characters required" });

            attendance.CheckOutTime = await CompanyNow();
            attendance.TotalHours = CalculateWorkingHours(attendance);
            attendance.EffectiveHours = CalculateEffectiveHours(attendance);
            var requiredHours = await StandardHours();
            attendance.OvertimeHours = attendance.EffectiveHours > requiredHours ? Math.Round(attendance.EffectiveHours - requiredHours, 2) : 0;
            attendance.AttendanceStatus = ResolveFinalStatus(attendance, requiredHours);
            attendance.Status = attendance.AttendanceStatus;
            attendance.WorkSummary = workSummary.Trim();

            _context.ActivityLogs.Add(new ActivityLog { UserId = attendance.UserId, Action = "Check Out", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            return Ok(new { message = "Check-out successful", workingHours = attendance.TotalHours, breakMinutes = attendance.BreakMinutes, breakHours = Math.Round(attendance.BreakMinutes / 60.0, 2), effectiveHours = attendance.EffectiveHours, overtimeHours = attendance.OvertimeHours, lateMinutes = attendance.LateMinutes, attendanceStatus = attendance.AttendanceStatus });
        }

        [HttpGet("today")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> Today()
        {
            return Ok(await TodayAttendance());
        }

        [HttpGet("my-attendance")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> MyAttendance()
        {
            var userId = CurrentUserId();
            var attendance = await _context.Attendances.Where(x => x.UserId == userId).OrderByDescending(x => x.AttendanceDate).ToListAsync();
            return Ok(attendance);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,HR")]
        public async Task<IActionResult> GetAllAttendance(string? search, string? status, DateTime? from, DateTime? to, int? departmentId, int? managerId, int page = 1, int pageSize = 500)
        {
            page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 500);
            var query = _context.Attendances.AsNoTracking().Where(x => x.User != null && x.User.Role != "Admin");
            if (User.IsInRole("Manager")) { var current = CurrentUserId(); query = query.Where(x => x.User!.Role == "Employee" && x.User.ManagerId == current); } else if (User.IsInRole("HR")) { var current = CurrentUserId(); query = query.Where(x => x.User!.AssignedHrId == current && (x.User.Role == "Employee" || x.User.Role == "Manager")); }
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.User!.FullName.Contains(search) || (x.User.EmployeeCode != null && x.User.EmployeeCode.Contains(search)));
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.AttendanceStatus == status || x.Status == status);
            if (from.HasValue) query = query.Where(x => x.AttendanceDate >= from.Value.Date);
            if (to.HasValue) query = query.Where(x => x.AttendanceDate < to.Value.Date.AddDays(1));
            if (departmentId.HasValue) query = query.Where(x => x.User!.DepartmentId == departmentId);
            if (managerId.HasValue) query = query.Where(x => x.User!.ManagerId == managerId);
            var totalCount = await query.CountAsync();
            var items = await query.OrderByDescending(x => x.AttendanceDate).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.AttendanceId, x.UserId, EmployeeName = x.User!.FullName, x.User.EmployeeCode, DepartmentId = x.User.DepartmentId, DepartmentName = x.User.Department != null ? x.User.Department.Name : null, ManagerId = x.User.ManagerId, ManagerName = x.User.Manager != null ? x.User.Manager.FullName : null, x.AttendanceDate, x.CheckInTime, x.CheckOutTime, x.TotalHours, x.BreakMinutes, x.EffectiveHours, x.LateMinutes, x.OvertimeHours, x.AttendanceStatus, x.Status }).ToListAsync();
            return Ok(new { items, page, pageSize, totalCount });
        }

        [HttpGet("monthly-summary")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> MonthlySummary()
        {
            var userId = CurrentUserId();
            var month = DateTime.Now.Month;
            var year = DateTime.Now.Year;
            var attendance = await _context.Attendances.Where(x => x.UserId == userId && x.AttendanceDate.Month == month && x.AttendanceDate.Year == year).ToListAsync();
            return Ok(new
            {
                totalDays = attendance.Count,
                presentDays = attendance.Count(x => x.AttendanceStatus == "Present"),
                halfDays = attendance.Count(x => x.AttendanceStatus == "Half Day"),
                absentDays = attendance.Count(x => x.AttendanceStatus == "Absent"),
                totalHours = Math.Round(attendance.Sum(x => x.TotalHours), 2),
                effectiveHours = Math.Round(attendance.Sum(x => x.EffectiveHours), 2),
                breakMinutes = attendance.Sum(x => x.BreakMinutes),
                overtimeHours = Math.Round(attendance.Sum(x => x.OvertimeHours), 2),
                lateDays = attendance.Count(x => x.LateMinutes > 0),
                requiredDailyHours = await StandardHours()
            });
        }

        [HttpGet("calendar")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> AttendanceCalendar()
        {
            var userId = CurrentUserId();
            var data = await _context.Attendances
                .Where(x => x.UserId == userId)
                .Select(x => new { x.AttendanceDate, x.AttendanceStatus, x.TotalHours, x.EffectiveHours, x.BreakMinutes, x.CheckInTime, x.CheckOutTime })
                .ToListAsync();
            return Ok(data);
        }

        [HttpPost("correction-request")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> CorrectionRequest(AttendanceCorrectionDto dto)
        {
            var userId = CurrentUserId();
            var attendance = await _context.Attendances.FirstOrDefaultAsync(x => x.AttendanceId == dto.AttendanceId && x.UserId == userId);
            if (attendance == null) return NotFound(new { message = "Attendance record not found" });
            if (string.IsNullOrWhiteSpace(dto.Reason) || dto.Reason.Trim().Length < 10) return BadRequest(new { message = "Reason must be at least 10 characters" });
            if (dto.RequestedCheckOut <= dto.RequestedCheckIn) return BadRequest(new { message = "Requested checkout must be after requested check-in" });
            if (dto.RequestedCheckIn.Date != attendance.AttendanceDate.Date || dto.RequestedCheckOut.Date != attendance.AttendanceDate.Date) return BadRequest(new { message = "Correction times must be on the attendance date" });
            if ((dto.RequestedCheckOut - dto.RequestedCheckIn).TotalHours > 24) return BadRequest(new { message = "Attendance duration cannot exceed 24 hours" });

            var exists = await _context.AttendanceRegularizations.AnyAsync(x => x.AttendanceId == dto.AttendanceId && x.UserId == userId && x.Status == "Pending");
            if (exists) return BadRequest(new { message = "A pending correction request already exists for this record" });

            _context.AttendanceRegularizations.Add(new AttendanceRegularization
            {
                UserId = userId,
                AttendanceId = dto.AttendanceId,
                RequestedCheckIn = dto.RequestedCheckIn,
                RequestedCheckOut = dto.RequestedCheckOut,
                Reason = dto.Reason.Trim(),
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            });
            _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = "Attendance Correction Requested", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Attendance correction request submitted" });
        }

        private async Task<Attendance?> TodayAttendance()
        {
            var userId = CurrentUserId();
            var today = DateTime.Today;
            return await _context.Attendances.FirstOrDefaultAsync(x => x.UserId == userId && x.AttendanceDate.Date == today);
        }

        private async Task<bool> SettingEnabled(string key)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(x => x.SettingKey == key);
            return bool.TryParse(setting?.SettingValue, out var enabled) && enabled;
        }

        private static string ResolveStatus(TimeSpan checkIn)
        {
            if (checkIn <= new TimeSpan(9, 40, 0)) return "Present";
            if (checkIn <= new TimeSpan(10, 0, 0)) return "Late";
            if (checkIn <= new TimeSpan(12, 0, 0)) return "Half Day";
            return "Absent";
        }

        private static string ResolveFinalStatus(Attendance attendance, double requiredHours)
        {
            if (attendance.AttendanceStatus == "Absent") return "Incomplete";
            if (attendance.AttendanceStatus == "Half Day") return "Half Day";
            if (attendance.EffectiveHours >= requiredHours) return attendance.LateMinutes > 0 ? "Late" : "Present";
            if (attendance.EffectiveHours >= 4) return "Half Day";
            return "Absent";
        }

        private async Task<DateTime> CompanyNow()
        {
            var zoneName = await _context.SystemSettings.Where(x => x.SettingKey == "CompanyTimezone").Select(x => x.SettingValue).FirstOrDefaultAsync() ?? "India Standard Time";
            try { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zoneName)); }
            catch { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")); }
        }

        private async Task<double> StandardHours()
        {
            var value = await _context.SystemSettings.Where(x => x.SettingKey == "StandardWorkHours").Select(x => x.SettingValue).FirstOrDefaultAsync();
            return double.TryParse(value, out var hours) && hours > 0 && hours <= 24 ? hours : 9;
        }

        private static double CalculateWorkingHours(Attendance attendance)
        {
            if (!attendance.CheckInTime.HasValue) return 0;
            var end = attendance.CheckOutTime ?? DateTime.Now;
            return Math.Round(Math.Max(0, (end - attendance.CheckInTime.Value).TotalHours), 2);
        }

        private static double CalculateEffectiveHours(Attendance attendance)
        {
            return Math.Round(Math.Max(0, CalculateWorkingHours(attendance) - (attendance.BreakMinutes / 60.0)), 2);
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }
}
