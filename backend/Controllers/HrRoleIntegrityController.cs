using System.Security.Claims;
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
[Route("api/hr")]
[Authorize(Roles = "HR")]
public sealed class HrRoleIntegrityController(
    ApplicationDbContext db,
    PasswordService passwords,
    IAuditService audit,
    ILogger<HrRoleIntegrityController>? logger = null) : ControllerBase
{
    private int HrId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<User> Owned(string role) => db.Users.Where(x => x.AssignedHrId == HrId() && x.Role == role);

    [HttpGet("employees")]
    public Task<IActionResult> Employees(string? search = null, string? status = null, int? departmentId = null, int? managerId = null, string sort = "name", int page = 1, int pageSize = 10) =>
        List("Employee", search, status, departmentId, managerId, sort, page, pageSize);

    [HttpGet("managers")]
    public Task<IActionResult> Managers(string? search = null, string? status = null, int? departmentId = null, string sort = "name", int page = 1, int pageSize = 10) =>
        List("Manager", search, status, departmentId, null, sort, page, pageSize);

    [HttpPost("employees")]
    public Task<IActionResult> CreateEmployee(CreateHrEmployeeDto dto, CancellationToken cancellationToken) =>
        Create(dto, "Employee", dto.Password, dto.ManagerId, cancellationToken);

    [HttpPost("managers")]
    public Task<IActionResult> CreateManager(CreateHrManagerDto dto, CancellationToken cancellationToken) =>
        Create(dto, "Manager", dto.Password, null, cancellationToken);

    [HttpPut("employees/{id:int}")]
    public Task<IActionResult> UpdateEmployee(int id, UpdateHrEmployeeDto dto) => Update(id, dto, "Employee", dto.ManagerId);

    [HttpPut("managers/{id:int}")]
    public Task<IActionResult> UpdateManager(int id, UpdateHrManagerDto dto) => Update(id, dto, "Manager", null);

    [HttpPatch("employees/{id:int}/status")]
    public Task<IActionResult> EmployeeStatus(int id, UserStatusRequest request) => Status(id, "Employee", request.IsActive);

    [HttpPatch("managers/{id:int}/status")]
    public Task<IActionResult> ManagerStatus(int id, UserStatusRequest request) => Status(id, "Manager", request.IsActive);

    private async Task<IActionResult> List(string role, string? search, string? status, int? departmentId, int? managerId, string sort, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var query = Owned(role).AsNoTracking().Include(x => x.Department).Include(x => x.Manager).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(x => x.FullName.Contains(value) || x.Email.Contains(value) || (x.EmployeeCode ?? "").Contains(value));
        }
        if (status == "Active") query = query.Where(x => x.IsActive);
        else if (status == "Inactive") query = query.Where(x => !x.IsActive);
        if (departmentId.HasValue) query = query.Where(x => x.DepartmentId == departmentId);
        if (role == "Employee" && managerId.HasValue) query = query.Where(x => x.ManagerId == managerId);
        query = sort == "joining" ? query.OrderByDescending(x => x.DateOfJoining) : sort == "code" ? query.OrderBy(x => x.EmployeeCode) : query.OrderBy(x => x.FullName);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(x => new
        {
            x.UserId, x.FullName, x.EmployeeCode, x.Email, x.Username, x.PhoneNumber, x.Role,
            department = x.Department != null ? x.Department.Name : null, x.DepartmentId, x.Designation,
            managerName = role == "Employee" && x.Manager != null ? x.Manager.FullName : null, x.ManagerId,
            x.EmploymentType, x.WorkLocation, x.DateOfJoining, x.EmploymentStatus, x.IsActive,
            teamMemberCount = role == "Manager" ? db.Users.Count(u => u.ManagerId == x.UserId && u.AssignedHrId == HrId() && u.Role == "Employee") : 0
        }).ToListAsync();
        return Ok(new { items, totalCount = total, page, pageSize, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    private async Task<IActionResult> Create(HrPersonWriteDto dto, string role, string password, int? managerId, CancellationToken cancellationToken)
    {
        var invalid = await Validate(dto, null, role, managerId, true, password, cancellationToken);
        if (invalid != null) return invalid;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var user = new User
            {
                FullName = dto.FullName.Trim(), EmployeeCode = dto.EmployeeCode.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(), Username = dto.Username.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(), DepartmentId = dto.DepartmentId,
                Designation = dto.Designation?.Trim(), DateOfJoining = dto.JoiningDate,
                EmploymentType = dto.EmploymentType, WorkLocation = dto.Location?.Trim(), Role = role,
                ManagerId = role == "Employee" ? managerId : null, AssignedHrId = HrId(),
                PasswordHash = passwords.HashPassword(password), EmploymentStatus = "Active", IsActive = dto.IsActive
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            await audit.RecordAsync(role + "Created", "User", user.UserId.ToString(), actorUserId: HrId(), safeMetadata: new { user.EmployeeCode, user.DepartmentId }, cancellationToken: cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new { message = role + " created", user.UserId });
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger?.LogWarning(exception, "Database constraint prevented {Role} creation by HR {HrId}", role, HrId());
            return Conflict(new { message = "Employee could not be created because an email, username, employee code, or related record is no longer valid." });
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            var correlationId = HttpContext.TraceIdentifier;
            logger?.LogError(exception, "Unexpected {Role} creation failure by HR {HrId}. CorrelationId {CorrelationId}", role, HrId(), correlationId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Employee creation failed unexpectedly.", correlationId });
        }
    }

    private async Task<IActionResult> Update(int id, HrPersonWriteDto dto, string role, int? managerId)
    {
        var user = await Owned(role).FirstOrDefaultAsync(x => x.UserId == id);
        if (user == null) return NotFound(new { message = role + " not found" });
        var invalid = await Validate(dto, id, role, managerId, false, null, CancellationToken.None);
        if (invalid != null) return invalid;
        user.FullName = dto.FullName.Trim(); user.PhoneNumber = dto.PhoneNumber?.Trim(); user.DepartmentId = dto.DepartmentId;
        user.Designation = dto.Designation?.Trim(); user.DateOfJoining = dto.JoiningDate; user.EmploymentType = dto.EmploymentType;
        user.WorkLocation = dto.Location?.Trim(); user.ManagerId = role == "Employee" ? managerId : null;
        await db.SaveChangesAsync();
        await audit.RecordAsync(role + "Updated", "User", id.ToString(), actorUserId: HrId());
        return Ok(new { message = role + " updated" });
    }

    private async Task<IActionResult> Status(int id, string role, bool active)
    {
        var user = await Owned(role).FirstOrDefaultAsync(x => x.UserId == id);
        if (user == null) return NotFound(new { message = role + " not found" });
        user.IsActive = active; user.EmploymentStatus = active ? "Active" : "Inactive";
        await db.SaveChangesAsync();
        await audit.RecordAsync(role + (active ? "Activated" : "Deactivated"), "User", id.ToString(), actorUserId: HrId());
        return Ok(new { message = role + (active ? " activated" : " deactivated") });
    }

    private async Task<IActionResult?> Validate(HrPersonWriteDto dto, int? id, string role, int? managerId, bool creating, string? password, CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var username = dto.Username.Trim().ToLowerInvariant();
        var code = dto.EmployeeCode.Trim().ToLowerInvariant();
        var otherUsers = db.Users.Where(x => !id.HasValue || x.UserId != id.Value);
        if (await otherUsers.AnyAsync(x => x.Email.ToLower() == email, cancellationToken)) return Conflict(new { message = "Official email already exists." });
        if (await otherUsers.AnyAsync(x => x.Username.ToLower() == username, cancellationToken)) return Conflict(new { message = "Username already exists." });
        if (await otherUsers.AnyAsync(x => (x.EmployeeCode ?? "").ToLower() == code, cancellationToken)) return Conflict(new { message = "Employee code already exists." });
        if (dto.DepartmentId.HasValue && !await db.Departments.AnyAsync(x => x.Id == dto.DepartmentId && x.IsActive, cancellationToken)) return BadRequest(new { message = "Select an active department." });
        if (role == "Employee" && managerId.HasValue)
        {
            var manager = await Owned("Manager").AsNoTracking().FirstOrDefaultAsync(x => x.UserId == managerId && x.IsActive, cancellationToken);
            if (manager == null) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Assigned manager must be an active Manager owned by this HR." });
            if (dto.DepartmentId.HasValue && manager.DepartmentId != dto.DepartmentId) return BadRequest(new { message = "Assigned manager must belong to the selected department." });
        }
        if (creating && (password!.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)))
            return BadRequest(new { message = "Temporary password must contain at least eight characters, including uppercase, lowercase, and a number." });
        return null;
    }
}
