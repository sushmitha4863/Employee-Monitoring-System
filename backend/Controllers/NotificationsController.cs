using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace EmployeeMonitoringAPI.Controllers;
[Route("api/[controller]")][ApiController][Authorize]
public sealed class NotificationsController:ControllerBase
{
 private readonly ApplicationDbContext _db;public NotificationsController(ApplicationDbContext db){_db=db;}
 [HttpGet]
 public async Task<IActionResult> Mine(bool? isRead,string? type,int page=1,int pageSize=10){page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var id=CurrentUserId();var q=_db.Notifications.AsNoTracking().Where(x=>x.UserId==id);if(isRead.HasValue)q=q.Where(x=>x.IsRead==isRead);if(!string.IsNullOrWhiteSpace(type))q=q.Where(x=>x.Type==type);var total=await q.CountAsync();var items=await q.OrderByDescending(x=>x.CreatedAt).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{notificationId=x.Id,id=x.Id,x.Title,x.Message,x.Type,x.RelatedUrl,x.IsRead,x.CreatedAt}).ToListAsync();return Ok(new PageResult<object>{Items=items.Cast<object>().ToList(),Page=page,PageSize=pageSize,TotalCount=total});}
 [HttpGet("unread-count")]
 public async Task<IActionResult> Unread()=>Ok(new{count=await _db.Notifications.CountAsync(x=>x.UserId==CurrentUserId()&&!x.IsRead)});
 [HttpPut("{id:int}/read")]
 public async Task<IActionResult> Read(int id){var item=await _db.Notifications.FirstOrDefaultAsync(x=>x.Id==id&&x.UserId==CurrentUserId());if(item is null)return NotFound(new{message="Notification not found"});item.IsRead=true;await _db.SaveChangesAsync();return Ok(new{message="Notification marked as read"});}
 [HttpPut("read-all")]
 public async Task<IActionResult> ReadAll(){await _db.Notifications.Where(x=>x.UserId==CurrentUserId()&&!x.IsRead).ExecuteUpdateAsync(x=>x.SetProperty(n=>n.IsRead,true));return Ok(new{message="All notifications marked as read"});}
 [HttpPost][Authorize(Roles="Admin,HR,Manager")]
 public async Task<IActionResult> Create(NotificationDto dto){if(string.IsNullOrWhiteSpace(dto.Title)||string.IsNullOrWhiteSpace(dto.Message))return BadRequest(new{message="Title and message are required"});var actorId=CurrentUserId();var q=_db.Users.Where(x=>x.IsActive);if(User.IsInRole("Manager"))q=q.Where(x=>x.Role=="Employee"&&x.ManagerId==actorId);else if(User.IsInRole("HR"))q=q.Where(x=>x.AssignedHrId==actorId&&(x.Role=="Employee"||x.Role=="Manager"));if(dto.UserId.HasValue){if(!await q.AnyAsync(x=>x.UserId==dto.UserId.Value))return Forbid();q=q.Where(x=>x.UserId==dto.UserId.Value);}else if(!string.IsNullOrWhiteSpace(dto.Role))q=q.Where(x=>x.Role==dto.Role);var ids=await q.Select(x=>x.UserId).ToListAsync();if(ids.Count==0)return NotFound(new{message="No recipients found"});_db.Notifications.AddRange(ids.Select(id=>new Notification{UserId=id,Title=dto.Title.Trim(),Message=dto.Message.Trim(),Type="General",CreatedAt=DateTime.UtcNow}));await _db.SaveChangesAsync();return Ok(new{message="Notification sent",recipients=ids.Count});}
 private int CurrentUserId()=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}