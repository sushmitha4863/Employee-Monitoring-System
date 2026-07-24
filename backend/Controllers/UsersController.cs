using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using EmployeeMonitoringAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private static readonly HashSet<string> ManageableRoles = new(StringComparer.OrdinalIgnoreCase) { "Employee", "Manager" };
        private readonly ApplicationDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly IAuditService _audit;

        public UsersController(ApplicationDbContext context, PasswordService passwordService, IAuditService audit)
        {
            _context = context;
            _passwordService = passwordService;
            _audit = audit;
        }

        [HttpGet]
        [Authorize(Roles = "Manager,HR")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await LoadManageableUsersQuery().ToListAsync();
            var teamCounts = await LoadTeamCounts();
            return Ok(users.Select(x => MapUser(x, teamCounts.GetValueOrDefault(x.UserId, 0))));
        }

        [HttpGet("employees")]
        [Authorize(Roles = "Manager,HR")]
        public async Task<IActionResult> GetEmployees()
        {
            var users = await LoadManageableUsersQuery().Where(x => x.Role == "Employee").ToListAsync();
            return Ok(users.Select(x => MapUser(x, 0)));
        }

        [HttpGet("managers")]
        [Authorize(Roles = "Admin,Manager,HR")]
        public async Task<IActionResult> GetManagers()
        {
            var managers = await LoadManageableUsersQuery().Where(x => x.Role == "Manager").ToListAsync();
            var teamCounts = await LoadTeamCounts();
            return Ok(managers.Select(x => MapUser(x, teamCounts.GetValueOrDefault(x.UserId, 0))));
        }

        [HttpGet("hr")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetHrUsers()
        {
            var users = await LoadManageableUsersQuery().Where(x => x.Role == "HR").ToListAsync();
            return Ok(users.Select(x => MapUser(x, 0)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await LoadManageableUsersQuery().FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null) return NotFound(new { message = "User not found" });

            var teamSize = await _context.Users.CountAsync(employee => employee.ManagerId == id && employee.Role == "Employee");
            return Ok(MapUser(user, teamSize));
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
        {
            var validation = await ValidateUserRequest(dto.Role, dto.Email, dto.DepartmentId, dto.ManagerId, null);
            if (validation != null) return validation;

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Username = dto.Username,
                Role = string.IsNullOrWhiteSpace(dto.Role) ? "Employee" : dto.Role,
                EmployeeCode = dto.EmployeeCode,
                PhoneNumber = dto.PhoneNumber,
                Designation = dto.Designation,
                DateOfJoining = dto.DateOfJoining,
                DepartmentId = dto.DepartmentId,
                ManagerId = dto.ManagerId,
                AssignedHrId = CurrentUserId(),
                PasswordHash = _passwordService.HashPassword(dto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User created successfully", user.UserId });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id && x.Role != "Admin" && x.AssignedHrId == CurrentUserId());
            if (user == null) return NotFound(new { message = "User not found" });

            if (!string.Equals(dto.Email, user.Email, StringComparison.OrdinalIgnoreCase) || dto.Role != user.Role || dto.Username != user.Username || dto.EmployeeCode != user.EmployeeCode) return BadRequest(new { message = "Official email, role, username and employee code are immutable" });
            var validation = await ValidateUserRequest(dto.Role, dto.Email, dto.DepartmentId, dto.ManagerId, id);
            if (validation != null) return validation;

            user.FullName = dto.FullName;
            user.IsActive = dto.IsActive;
            user.PhoneNumber = dto.PhoneNumber;
            user.Designation = dto.Designation;
            user.DateOfJoining = dto.DateOfJoining;
            user.DateOfBirth = dto.DateOfBirth;
            user.Address = dto.Address;
            user.DepartmentId = dto.DepartmentId;
            user.ManagerId = dto.ManagerId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "User updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public IActionResult DeleteUser(int id) => Conflict(new { message = "Employment records cannot be permanently deleted. Use offboarding." });
        [HttpPut("{id}/status")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> SetStatus(int id, UserStatusRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id && x.Role != "Admin" && x.AssignedHrId == CurrentUserId());
            if (user == null) return NotFound(new { message = "User not found" });
            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { message = request.IsActive ? "User activated" : "User deactivated" });
        }

        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "HR")]
        public IActionResult ResetPassword(int id, ResetPasswordRequest request) => StatusCode(StatusCodes.Status403Forbidden, new { message = "HR password resets require a separate authorized support workflow." });
        [HttpPost("change-password")]
        [EnableRateLimiting("password-change")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); if (!int.TryParse(userId, out var id)) return Unauthorized();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id); if (user == null) return NotFound();
            if (user.PasswordChangeLockedUntil > DateTime.UtcNow) return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Password change is temporarily locked." });
            if (!_passwordService.VerifyPassword(user.PasswordHash, request.OldPassword))
            {
                user.FailedPasswordChangeAttempts++; if (user.FailedPasswordChangeAttempts >= 5) user.PasswordChangeLockedUntil = DateTime.UtcNow.AddMinutes(15); await _context.SaveChangesAsync(); await _audit.RecordAsync("PasswordChangeFailure", "User", id.ToString(), "Failure", actorUserId: id); return BadRequest(new { message = "Current password is incorrect" });
            }
            if (request.NewPassword.Length < 8 || !request.NewPassword.Any(char.IsUpper) || !request.NewPassword.Any(char.IsLower) || !request.NewPassword.Any(char.IsDigit)) return BadRequest(new { message = "New password must contain at least 8 characters, uppercase, lowercase and a number." });
            if (_passwordService.VerifyPassword(user.PasswordHash, request.NewPassword)) return Conflict(new { message = "New password must differ from the current password." });
            if (!string.IsNullOrEmpty(request.ConfirmNewPassword) && request.NewPassword != request.ConfirmNewPassword) return BadRequest(new { message = "Password confirmation does not match." });
            user.PasswordHash = _passwordService.HashPassword(request.NewPassword); user.LastPasswordChangedAt = DateTime.UtcNow; user.FailedPasswordChangeAttempts = 0; user.PasswordChangeLockedUntil = null; await _context.SaveChangesAsync(); await _audit.RecordAsync("PasswordChanged", "User", id.ToString(), actorUserId: id); return Ok(new { message = "Password changed successfully" });
        }

        private IQueryable<User> LoadManageableUsersQuery()
        {
            var query = _context.Users.Include(x => x.Department).Include(x => x.Manager).Where(x => x.Role != "Admin");
            if (User.IsInRole("HR")) query = query.Where(x => x.AssignedHrId == CurrentUserId() || x.UserId == CurrentUserId());
            if (User.IsInRole("Manager")) query = query.Where(x => x.ManagerId == CurrentUserId() && x.Role == "Employee");
            return query;
        }

        private async Task<Dictionary<int, int>> LoadTeamCounts()
        {
            return await _context.Users
                .Where(x => x.ManagerId.HasValue && x.Role == "Employee" && (!User.IsInRole("HR") || x.AssignedHrId == CurrentUserId()))
                .GroupBy(x => x.ManagerId!.Value)
                .Select(x => new { ManagerId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.ManagerId, x => x.Count);
        }

        private async Task<IActionResult?> ValidateUserRequest(string? role, string email, int? departmentId, int? managerId, int? userId)
        {
            var normalizedRole = string.IsNullOrWhiteSpace(role) ? "Employee" : role;
            if (!ManageableRoles.Contains(normalizedRole)) return BadRequest(new { message = "Only Employee, Manager and HR users can be managed here" });

            var emailExists = await _context.Users.AnyAsync(x => x.Email == email && (!userId.HasValue || x.UserId != userId.Value));
            if (emailExists) return BadRequest(new { message = "Email already exists" });

            if (departmentId.HasValue && !await _context.Departments.AnyAsync(x => x.Id == departmentId.Value))
            {
                return BadRequest(new { message = "Department not found" });
            }

            if (managerId.HasValue)
            {
                var manager = await _context.Users.FirstOrDefaultAsync(x => x.UserId == managerId.Value && x.Role == "Manager" && x.IsActive && x.AssignedHrId == CurrentUserId());
                if (manager == null) return BadRequest(new { message = "Assigned manager must be an active manager" });
                if (userId.HasValue && managerId.Value == userId.Value) return BadRequest(new { message = "A user cannot be assigned as their own manager" });
            }

            return null;
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private static object MapUser(User x, int teamSize) => new
        {
            x.UserId,
            x.EmployeeCode,
            x.FullName,
            x.Email,
            x.Username,
            x.PhoneNumber,
            x.Designation,
            x.DateOfJoining,
            x.DateOfBirth,
            x.Address,
            x.Role,
            x.IsActive,
            x.ProfileImage,
            x.DepartmentId,
            x.ManagerId,
            Department = x.Department != null ? x.Department.Name : null,
            DepartmentName = x.Department != null ? x.Department.Name : null,
            Manager = x.Manager != null ? x.Manager.FullName : null,
            TeamSize = teamSize
        };
    }

    public class UserStatusRequest
    {
        public bool IsActive { get; set; }
    }
}

