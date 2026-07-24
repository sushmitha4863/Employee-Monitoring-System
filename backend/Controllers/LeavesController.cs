using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Models;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public partial class LeavesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public LeavesController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpPost]
        [HttpPost("apply")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> ApplyLeave(Leave leave)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var employee = await _context.Users.Include(x => x.Manager).FirstOrDefaultAsync(x => x.UserId == userId);
            if (employee == null || !employee.IsActive) return Unauthorized();
            if (!employee.ManagerId.HasValue || employee.Manager == null || !employee.Manager.IsActive || employee.Manager.Role != "Manager") return BadRequest(new { message = "An active assigned Manager is required before leave can be submitted" });

            if (leave.FromDate.Date < DateTime.Today) return BadRequest(new { message = "Cannot apply leave in the past" });
            if (leave.ToDate.Date < leave.FromDate.Date) return BadRequest(new { message = "To date must be after from date" });
            if (string.IsNullOrWhiteSpace(leave.LeaveType)) return BadRequest(new { message = "Leave type is required" });
            if (string.IsNullOrWhiteSpace(leave.Reason) || leave.Reason.Trim().Length < 10) return BadRequest(new { message = "Reason must be at least 10 characters" });

            var requestedDates = Enumerable.Range(0, (leave.ToDate.Date - leave.FromDate.Date).Days + 1).Select(offset => leave.FromDate.Date.AddDays(offset)).ToList();
            var holidayDates = await _context.CompanyHolidays.Where(x => x.IsActive && x.HolidayDate.Date >= leave.FromDate.Date && x.HolidayDate.Date <= leave.ToDate.Date).Select(x => x.HolidayDate.Date).ToListAsync();
            var businessDates = requestedDates.Where(x => x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday && !holidayDates.Contains(x.Date)).ToList();
            if (businessDates.Count == 0) return BadRequest(new { message = "The selected range contains no working days" });

            var overlap = await _context.Leaves.AnyAsync(x => x.UserId == userId && x.Status != "Rejected" && x.Status != "Cancelled" && x.FromDate <= leave.ToDate.Date && x.ToDate >= leave.FromDate.Date);
            if (overlap) return BadRequest(new { message = "Leave request overlaps with an existing leave" });

            leave.UserId = userId;
            leave.Status = "Pending";
            leave.AppliedAt = DateTime.UtcNow;
            leave.FromDate = leave.FromDate.Date;
            leave.ToDate = leave.ToDate.Date;
            leave.Reason = leave.Reason.Trim();

            var balance = await _context.LeaveBalances.FirstOrDefaultAsync(x => x.UserId == userId);
            if (balance == null)
            {
                balance = new LeaveBalance { UserId = userId, TotalLeaves = 12, UsedLeaves = 0 };
                _context.LeaveBalances.Add(balance);
            }

            var requestedDays = leave.IsHalfDay ? 0.5m : businessDates.Count;
            if (!leave.LeaveType.Contains("Loss", StringComparison.OrdinalIgnoreCase) && requestedDays > balance.RemainingLeaves)
            {
                return BadRequest(new { message = "Leave request exceeds available balance" });
            }

            _context.Leaves.Add(leave);
            _context.LeaveHistories.Add(new LeaveHistory { Leave = leave, FromStatus = "Draft", ToStatus = "Pending", Remarks = "Submitted", ActorUserId = userId });
            _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = "Leave Applied", CreatedAt = DateTime.UtcNow });

            if (employee.ManagerId.HasValue)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = employee.ManagerId.Value,
                    Title = "Leave Applied",
                    Message = $"{employee.FullName} applied for {leave.LeaveType} from {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            if (employee.Manager != null && !string.IsNullOrWhiteSpace(employee.Manager.Email))
            {
                await _emailService.TrySendEmailAsync(employee.Manager.Email, "Leave Applied", $"<p>{employee.FullName} applied for {leave.LeaveType} from {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy}.</p>");
            }

            return Ok(new { message = "Leave applied successfully", leave.LeaveId });
        }

        [HttpGet("my-leaves")]
        public async Task<IActionResult> MyLeaves()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var leaves = await _context.Leaves.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.AppliedAt).Select(x => new { x.LeaveId,x.LeaveType,x.FromDate,x.ToDate,x.IsHalfDay,x.Reason,x.Status,x.AppliedAt,x.ApprovedAt,x.ApprovalRemarks,x.CancellationRequestedAt,x.CancellationReason,history=x.History.OrderBy(h=>h.ChangedAt).Select(h=>new{h.LeaveHistoryId,h.FromStatus,h.ToStatus,h.Remarks,h.ChangedAt,actor=h.ActorUser!.FullName}).ToList() }).ToListAsync();
            return Ok(leaves);
        }

        [HttpGet]
        [Authorize(Roles = "Manager,HR")]
        public async Task<IActionResult> GetAllLeaves()
        {
            var leaves = await _context.Leaves
                .Include(x => x.User)
                .Include(x => x.ApprovedBy)
                .OrderByDescending(x => x.AppliedAt)
                .Select(x => new
                {
                    x.LeaveId,
                    x.UserId,
                    EmployeeName = x.User!.FullName,
                    x.FromDate,
                    x.ToDate,
                    TotalDays = EF.Functions.DateDiffDay(x.FromDate, x.ToDate) + 1,
                    x.LeaveType,
                    x.Reason,
                    x.Status,
                    ApprovedBy = x.ApprovedBy != null ? x.ApprovedBy.FullName : null,
                    x.ApprovedAt,
                    x.ApprovalRemarks,
                    x.AppliedAt
                })
                .ToListAsync();

            return Ok(leaves);
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            var leave = await _context.Leaves.Include(x => x.User).FirstOrDefaultAsync(x => x.LeaveId == id);
            if (leave == null || leave.User?.ManagerId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return NotFound(new { message = "Leave not found" });
            if (leave.Status != "Pending") return Conflict(new { message = "Only pending leave can be approved" });

            var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var failure = await LeaveWorkflow.ReviewAsync(_context, leave, actorId, "Approved", "Approved");
            if (failure is not null) return StatusCode(failure.StatusCode, new { message = failure.Message });
            AddLeaveDecisionNotifications(leave, "Leave Approved", "approved");
            await _context.SaveChangesAsync();

            if (leave.User != null)
            {
                await _emailService.TrySendEmailAsync(leave.User.Email, "Leave Approved", $"<p>Your {leave.LeaveType} request has been approved.</p>");
            }

            return Ok(new { message = "Leave approved" });
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> RejectLeave(int id, LeaveDecisionRequest request)
        {
            var leave = await _context.Leaves.Include(x => x.User).FirstOrDefaultAsync(x => x.LeaveId == id);
            if (leave == null || leave.User?.ManagerId != int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)) return NotFound(new { message = "Leave not found" });
            if (leave.Status != "Pending") return Conflict(new { message = "Only pending leave can be rejected" });

            var actorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var failure = await LeaveWorkflow.ReviewAsync(_context, leave, actorId, "Rejected", request.Remarks);
            if (failure is not null) return StatusCode(failure.StatusCode, new { message = failure.Message });
            AddLeaveDecisionNotifications(leave, "Leave Rejected", "rejected");
            await _context.SaveChangesAsync();

            if (leave.User != null)
            {
                await _emailService.TrySendEmailAsync(leave.User.Email, "Leave Rejected", $"<p>Your {leave.LeaveType} request has been rejected.</p>");
            }

            return Ok(new { message = "Leave rejected" });
        }

        [HttpGet("summary")]
        public async Task<IActionResult> LeaveSummary()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var approvedLeaves = await _context.Leaves.CountAsync(x => x.UserId == userId && x.Status == "Approved");
            var pendingLeaves = await _context.Leaves.CountAsync(x => x.UserId == userId && x.Status == "Pending");
            var rejectedLeaves = await _context.Leaves.CountAsync(x => x.UserId == userId && x.Status == "Rejected");
            return Ok(new { approvedLeaves, pendingLeaves, rejectedLeaves });
        }

        [HttpGet("balance")]
        public async Task<IActionResult> LeaveBalance()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var balance = await _context.LeaveBalances.FirstOrDefaultAsync(x => x.UserId == userId);

            if (balance == null)
            {
                balance = new LeaveBalance { UserId = userId, TotalLeaves = 12, UsedLeaves = 0 };
                _context.LeaveBalances.Add(balance);
                await _context.SaveChangesAsync();
            }

            return Ok(balance);
        }


        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> CancelLeave(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var leave = await _context.Leaves.FirstOrDefaultAsync(x => x.LeaveId == id && x.UserId == userId);
            if (leave == null) return NotFound(new { message = "Leave not found" });
            if (leave.Status != "Pending") return BadRequest(new { message = "Only pending leave requests can be cancelled" });
            var previous = leave.Status; leave.Status = "Cancelled"; LeaveWorkflow.AddHistory(_context, leave, userId, previous, "Cancelled", "Cancelled by requester");
            _context.ActivityLogs.Add(new ActivityLog { UserId = userId, Action = "Leave Cancelled", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Leave cancelled" });
        }

        private async Task<int> BusinessDays(DateTime from, DateTime to, bool halfDay)
        {
            if (halfDay) return 1;
            var holidays = await _context.CompanyHolidays.Where(x => x.IsActive && x.HolidayDate >= from.Date && x.HolidayDate <= to.Date).Select(x => x.HolidayDate.Date).ToListAsync();
            return Enumerable.Range(0, (to.Date - from.Date).Days + 1).Select(i => from.Date.AddDays(i)).Count(x => x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday && !holidays.Contains(x));
        }

        private async Task<bool> SettingEnabled(string key)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(x => x.SettingKey == key);
            return bool.TryParse(setting?.SettingValue, out var enabled) && enabled;
        }
        private void AddLeaveDecisionNotifications(Leave leave, string title, string decision)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = leave.UserId,
                Title = title,
                Message = $"Your {leave.LeaveType} request from {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy} was {decision}.",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = leave.UserId,
                Action = title,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
    public sealed class LeaveDecisionRequest { public string? Remarks { get; set; } }
}



