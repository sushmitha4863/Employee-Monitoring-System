using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
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
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _environment;

        public TasksController(ApplicationDbContext context, EmailService emailService, IWebHostEnvironment environment)
        {
            _context = context;
            _emailService = emailService;
            _environment = environment;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,HR")]
        public async Task<IActionResult> GetTasks()
        {
            var query = _context.WorkTasks.AsQueryable();
            var currentUserId = CurrentUserId();
            if (User.IsInRole("Manager")) query = query.Where(x => x.AssignedTo != null && x.AssignedTo.Role == "Employee" && x.AssignedTo.ManagerId == currentUserId);
            else if (User.IsInRole("HR")) query = query.Where(x => x.AssignedTo != null && x.AssignedTo.AssignedHrId == currentUserId);
            var tasks = await query
                .Include(x => x.AssignedTo)
                .Include(x => x.Project)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    x.TaskId,
                    TaskName = x.Title,
                    x.Title,
                    x.Description,
                    x.Priority,
                    x.Status,
                    x.ProgressPercentage,
                    x.EmployeeRemarks,
                    x.DueDate,
                    x.CreatedAt,
                    x.CompletedAt,
                    AssignedEmployeeName = x.AssignedTo != null ? x.AssignedTo.FullName : null,
                    ProjectName = x.Project != null ? x.Project.ProjectName : null
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("my-tasks")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> MyTasks()
        {
            var userId = CurrentUserId();
            var tasks = await _context.WorkTasks
                .Include(x => x.Project)
                .Where(x => x.AssignedToId == userId)
                .OrderBy(x => x.DueDate)
                .Select(x => new
                {
                    x.TaskId,
                    TaskName = x.Title,
                    x.Title,
                    x.Description,
                    x.Priority,
                    x.Status,
                    x.ProgressPercentage,
                    x.EmployeeRemarks,
                    x.DueDate,
                    x.CreatedAt,
                    x.CompletedAt,
                    ProjectId = x.ProjectId,
                    ProjectName = x.Project != null ? x.Project.ProjectName : null,
                    AttachmentCount = _context.DocumentFiles.Count(d => d.Category == "Task-" + x.TaskId)
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTask(CreateTaskDto dto)
        {
            var assignedById = CurrentUserId();
            var assignee = await _context.Users.FirstOrDefaultAsync(x => x.UserId == dto.AssignedToId && x.IsActive && x.Role == "Employee");
            if (assignee == null) return NotFound(new { message = "Assigned user must be an active Employee" });
            if (User.IsInRole("Manager") && assignee.ManagerId != assignedById) return Forbid();
            if (User.IsInRole("Manager") && (!dto.ProjectId.HasValue || !await _context.Projects.AnyAsync(x => x.ProjectId == dto.ProjectId && x.ManagerId == assignedById && !x.IsArchived))) return Forbid();

            if (dto.ProjectId.HasValue)
            {
                var assignedToProject = await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == dto.ProjectId.Value && x.UserId == dto.AssignedToId);
                if (!assignedToProject) return BadRequest(new { message = "Task assignee must be assigned to the selected project" });
            }

            var task = new WorkTask
            {
                Title = dto.Title,
                Description = dto.Description,
                ExpectedOutput = dto.ExpectedOutput,
                ManagerNotes = dto.ManagerNotes,
                StartDate = dto.StartDate,
                AssignedToId = dto.AssignedToId,
                AssignedById = assignedById,
                ProjectId = dto.ProjectId,
                Priority = dto.Priority,
                DueDate = dto.DueDate,
                Status = "Assigned",
                ProgressPercentage = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkTasks.Add(task);
            _context.Notifications.Add(new Notification { UserId = assignee.UserId, Title = "Task assigned", Message = $"You have been assigned task {task.Title}.", Type = "TaskAssigned", RelatedUrl = "/employee/projects", DeduplicationKey = $"task-assigned:{task.TaskId}:{assignee.UserId}:{DateTime.UtcNow.Ticks}", IsRead = false, CreatedAt = DateTime.UtcNow });
            _context.ActivityLogs.Add(new ActivityLog { UserId = assignee.UserId, Action = $"Task Assigned: {task.Title}", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            await _emailService.TrySendEmailAsync(assignee.Email, "Task Assigned", $"<p>You have been assigned task <strong>{task.Title}</strong>.</p>");
            return Ok(new { message = "Task created", task.TaskId });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTask(int id, CreateTaskDto dto)
        {
            var task = await _context.WorkTasks.FirstOrDefaultAsync(x => x.TaskId == id);
            if (task == null) return NotFound(new { message = "Task not found" });

            var assignee = await _context.Users.FirstOrDefaultAsync(x => x.UserId == dto.AssignedToId && x.IsActive && x.Role == "Employee");
            if (assignee == null) return BadRequest(new { message = "Assigned user must be an active Employee" });
            if (dto.ProjectId.HasValue && !await _context.ProjectAssignments.AnyAsync(x => x.ProjectId == dto.ProjectId.Value && x.UserId == dto.AssignedToId))
                return BadRequest(new { message = "Task assignee must be assigned to the selected project" });

            task.Title = dto.Title;
            task.Description = dto.Description;
            task.ExpectedOutput = dto.ExpectedOutput;
            task.ManagerNotes = dto.ManagerNotes;
            task.StartDate = dto.StartDate;
            task.AssignedToId = dto.AssignedToId;
            task.ProjectId = dto.ProjectId;
            task.Priority = dto.Priority;
            task.DueDate = dto.DueDate;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Task updated" });
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.WorkTasks.FirstOrDefaultAsync(x => x.TaskId == id);
            if (task == null) return NotFound(new { message = "Task not found" });
            _context.WorkTasks.Remove(task);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Task deleted" });
        }
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] TaskStatusRequest request)
        {
            var managerId=CurrentUserId();var task=await _context.WorkTasks.Include(x=>x.AssignedTo).FirstOrDefaultAsync(x=>x.TaskId==id&&x.AssignedTo!.ManagerId==managerId);if(task is null)return NotFound(new{message="Task not found"});
            var status=request.Status?.Trim();if(status is not ("Completed" or "Reopened" or "Cancelled"))return BadRequest(new{message="Manager status must be Completed, Reopened or Cancelled"});
            if(status=="Completed"&&task.Status!="Ready for Review")return Conflict(new{message="Only tasks ready for review can be completed"});
            if(status=="Reopened"&&task.Status is not ("Ready for Review" or "Completed"))return Conflict(new{message="Only reviewed tasks can be reopened"});
            task.Status=status;task.ManagerFeedback=request.Feedback?.Trim();task.CompletedAt=status=="Completed"?DateTime.UtcNow:null;if(status=="Reopened")task.SubmittedAt=null;
            _context.Notifications.Add(new Notification{UserId=task.AssignedToId,Title=$"Task {status.ToLowerInvariant()}",Message=$"{task.Title} was {status.ToLowerInvariant()} by your Manager. {task.ManagerFeedback}".Trim(),Type=$"Task{status}",RelatedUrl="/employee/projects",DeduplicationKey=$"task-review:{task.TaskId}:{status}:{DateTime.UtcNow.Ticks}"});
            await _context.SaveChangesAsync();return Ok(new{message=$"Task {status.ToLowerInvariant()}",task.Status,task.ManagerFeedback});
        }

        [HttpPut("{id}/progress")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> UpdateTaskProgress(int id, [FromBody] TaskProgressDto request)
        {
            return await UpdateStatus(id,new TaskStatusRequest{Status=request.Status,Feedback=request.Remarks});
        }
        [HttpPost("{id}/attachments")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> UploadAttachment(int id, IFormFile file)
        {
            var userId = CurrentUserId();
            var task = await _context.WorkTasks.FirstOrDefaultAsync(x => x.TaskId == id);
            if (task == null) return NotFound(new { message = "Task not found" });
            if (!await CanAccessTask(task, userId)) return Forbid();
            if (file == null || file.Length == 0) return BadRequest(new { message = "File is required" });

            var uploadsRoot = Path.Combine(_environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "documents");
            Directory.CreateDirectory(uploadsRoot);
            var storedName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var fullPath = Path.Combine(uploadsRoot, storedName);
            await using (var stream = new FileStream(fullPath, FileMode.Create)) await file.CopyToAsync(stream);

            var document = new DocumentFile { FileName = storedName, OriginalFileName = file.FileName, ContentType = file.ContentType, Size = file.Length, Category = $"Task-{id}", FilePath = fullPath, UploadedById = userId, UploadedAt = DateTime.UtcNow };
            _context.DocumentFiles.Add(document);
            _context.ActivityLogs.Add(new ActivityLog { UserId = task.AssignedToId, Action = $"Task Attachment Uploaded: {task.Title}", CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Attachment uploaded", document.DocumentId });
        }

        [HttpGet("{id}/attachments")]
        [Authorize(Roles = "Employee,Manager,HR")]
        public async Task<IActionResult> Attachments(int id)
        {
            var userId = CurrentUserId();
            var task = await _context.WorkTasks.FirstOrDefaultAsync(x => x.TaskId == id);
            if (task == null) return NotFound(new { message = "Task not found" });
            if (!await CanAccessTask(task, userId)) return Forbid();
            var docs = await _context.DocumentFiles.Where(x => x.Category == $"Task-{id}").OrderByDescending(x => x.UploadedAt).Select(x => new { x.DocumentId, x.OriginalFileName, x.ContentType, x.Size, x.UploadedAt }).ToListAsync();
            return Ok(docs);
        }

        private async Task<bool> CanAccessTask(WorkTask task, int userId)
        {
            if (User.IsInRole("Admin") || task.AssignedToId == userId) return true;
            if (User.IsInRole("Manager")) return await _context.Users.AnyAsync(x => x.UserId == task.AssignedToId && x.Role == "Employee" && x.ManagerId == userId);
            if (User.IsInRole("HR")) return await _context.Users.AnyAsync(x => x.UserId == task.AssignedToId && x.AssignedHrId == userId);
            return false;
        }

        private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    public class TaskStatusRequest
    {
        public string Status { get; set; } = "Pending";
        public string? Feedback { get; set; }
    }
}
