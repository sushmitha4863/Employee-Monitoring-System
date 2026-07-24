using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Interfaces;
using EmployeeMonitoringAPI.Models;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMonitoringAPI.Controllers;

[ApiController]
[Route("api/admin/hr")]
[Authorize(Roles = "Admin")]
public sealed class AdminHrController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordService _passwords;
    private readonly IAuditService _audit;
    public AdminHrController(ApplicationDbContext db, PasswordService passwords, IAuditService audit) { _db = db; _passwords = passwords; _audit = audit; }

    [HttpGet]
    public async Task<IActionResult> List(string? search = null, int? departmentId = null, bool? isActive = null, int page = 1, int pageSize = 10)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 5, 100);
        var query = _db.Users.AsNoTracking().Include(x => x.Department).Where(x => x.Role == "HR");
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(x => x.FullName.Contains(term) || x.Email.Contains(term) || (x.EmployeeCode ?? "").Contains(term)); }
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive);
        var total = await query.CountAsync();
        var items = await query.OrderBy(x => x.FullName).Skip((page - 1) * pageSize).Take(pageSize).Select(x => new { x.UserId, x.FullName, x.EmployeeCode, x.Email, x.PhoneNumber, x.DepartmentId, department = x.Department != null ? x.Department.Name : null, x.Designation, x.IsActive, x.CreatedAt, x.LastLoginAt }).ToListAsync();
        return Ok(new { items, totalCount = total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _db.Users.AsNoTracking().Include(x => x.Department).Where(x => x.UserId == id && x.Role == "HR").Select(x => new { x.UserId, x.FullName, x.EmployeeCode, x.Email, x.PhoneNumber, x.DepartmentId, department = x.Department != null ? x.Department.Name : null, x.Designation, x.IsActive, x.CreatedAt, x.LastLoginAt }).FirstOrDefaultAsync();
        return item is null ? NotFound(new { message = "HR user not found" }) : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateHrUserDto dto)
    {
        var validation = await Validate(dto.Email, dto.EmployeeCode, dto.DepartmentId, dto.Password, dto.ConfirmPassword, null); if (validation is not null) return validation;
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = new User { FullName = dto.FullName.Trim(), EmployeeCode = dto.EmployeeCode.Trim(), Email = email, Username = email, PhoneNumber = dto.PhoneNumber?.Trim(), DepartmentId = dto.DepartmentId, Designation = dto.Designation.Trim(), PasswordHash = _passwords.HashPassword(dto.Password), Role = "HR", IsActive = true, CreatedAt = DateTime.UtcNow };
        _db.Users.Add(user); await _db.SaveChangesAsync();
        _db.Notifications.Add(new Notification { UserId = user.UserId, Title = "HR account created", Message = "Your HR account is ready. Sign in and change your password after first access.", Type = "AccountCreated", RelatedUrl = "/hr/dashboard", DeduplicationKey = $"hr-created:{user.UserId}" });
        await _db.SaveChangesAsync(); await _audit.RecordAsync("HrUserCreated", "User", user.UserId.ToString(), safeMetadata: new { user.EmployeeCode, user.DepartmentId });
        return CreatedAtAction(nameof(Get), new { id = user.UserId }, new { user.UserId, message = "HR account created" });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateHrUserDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id && x.Role == "HR"); if (user is null) return NotFound(new { message = "HR user not found" });
        var validation = await Validate(dto.Email, dto.EmployeeCode, dto.DepartmentId, null, null, id); if (validation is not null) return validation;
        user.FullName = dto.FullName.Trim(); user.EmployeeCode = dto.EmployeeCode.Trim(); user.Email = dto.Email.Trim().ToLowerInvariant(); user.Username = user.Email; user.PhoneNumber = dto.PhoneNumber?.Trim(); user.DepartmentId = dto.DepartmentId; user.Designation = dto.Designation.Trim();
        await _db.SaveChangesAsync(); await _audit.RecordAsync("HrUserUpdated", "User", id.ToString(), safeMetadata: new { user.DepartmentId }); return Ok(new { message = "HR account updated" });
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Status(int id, UserStatusRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id && x.Role == "HR"); if (user is null) return NotFound(new { message = "HR user not found" });
        if (user.IsActive == request.IsActive) return Conflict(new { message = request.IsActive ? "HR account is already active" : "HR account is already inactive" });
        user.IsActive = request.IsActive; await _db.SaveChangesAsync(); await _audit.RecordAsync(request.IsActive ? "HrUserReactivated" : "HrUserDeactivated", "User", id.ToString()); return Ok(new { message = request.IsActive ? "HR account reactivated" : "HR account deactivated" });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id, AdminResetPasswordDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == id && x.Role == "HR"); if (user is null) return NotFound(new { message = "HR user not found" });
        var passwordError = PasswordError(dto.NewPassword, dto.ConfirmPassword); if (passwordError is not null) return BadRequest(new { message = passwordError });
        user.PasswordHash = _passwords.HashPassword(dto.NewPassword); user.LastPasswordChangedAt = DateTime.UtcNow; user.FailedPasswordChangeAttempts = 0; user.PasswordChangeLockedUntil = null;
        _db.Notifications.Add(new Notification { UserId = id, Title = "Password reset", Message = "An administrator reset your password. Change it after signing in.", Type = "PasswordReset", RelatedUrl = "/hr/settings", DeduplicationKey = $"hr-password-reset:{id}:{DateTime.UtcNow.Ticks}" });
        await _db.SaveChangesAsync(); await _audit.RecordAsync("HrPasswordReset", "User", id.ToString()); return Ok(new { message = "Password reset successfully" });
    }

    private async Task<IActionResult?> Validate(string email, string employeeCode, int departmentId, string? password, string? confirmation, int? excludedId)
    {
        email = email.Trim().ToLowerInvariant(); employeeCode = employeeCode.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email.ToLower() == email && (!excludedId.HasValue || x.UserId != excludedId))) return Conflict(new { message = "Email already exists" });
        if (await _db.Users.AnyAsync(x => x.EmployeeCode != null && x.EmployeeCode.ToLower() == employeeCode && (!excludedId.HasValue || x.UserId != excludedId))) return Conflict(new { message = "Employee code already exists" });
        if (!await _db.Departments.AnyAsync(x => x.Id == departmentId && x.IsActive)) return BadRequest(new { message = "An active department is required" });
        var passwordError = password is null ? null : PasswordError(password, confirmation); return passwordError is null ? null : BadRequest(new { message = passwordError });
    }
    private static string? PasswordError(string password, string? confirmation) => password != confirmation ? "Password confirmation does not match" : password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) ? "Password must contain at least 8 characters, uppercase, lowercase and a number" : null;
}