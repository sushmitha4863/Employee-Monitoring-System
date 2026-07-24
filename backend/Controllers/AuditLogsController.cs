using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Helpers;
using EmployeeMonitoringAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EmployeeMonitoringAPI.Controllers;
[Route("api/[controller]")][ApiController][Authorize(Roles="Admin")]
public sealed class AuditLogsController:ControllerBase
{
 private readonly ApplicationDbContext _db;private readonly IAuditService _audit;public AuditLogsController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
 [HttpGet]
 public async Task<IActionResult> List(string? action,string? entityType,DateTime? from,DateTime? to,int page=1,int pageSize=15){page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,100);var q=Query(action,entityType,from,to);var total=await q.CountAsync();var items=await q.OrderByDescending(x=>x.Timestamp).Skip((page-1)*pageSize).Take(pageSize).Select(x=>new{x.Id,x.ActorUserId,ActorName=_db.Users.Where(u=>u.UserId==x.ActorUserId).Select(u=>u.FullName).FirstOrDefault(),x.Action,x.EntityType,x.EntityId,x.Timestamp,x.Result,x.SafeMetadata,x.CorrelationId,x.IpAddress,x.UserAgent}).ToListAsync();return Ok(new PageResult<object>{Items=items.Cast<object>().ToList(),Page=page,PageSize=pageSize,TotalCount=total});}
 [HttpGet("export")]
 public async Task<IActionResult> Export(string? action,string? entityType,DateTime? from,DateTime? to){var rows=await Query(action,entityType,from,to).OrderByDescending(x=>x.Timestamp).Take(10000).ToListAsync();var bytes=CsvSecurity.Build(new[]{"TimestampUtc","ActorUserId","Action","EntityType","EntityId","Result","CorrelationId","IpAddress"},rows.Select(x=>new string?[]{x.Timestamp.ToString("O"),x.ActorUserId?.ToString(),x.Action,x.EntityType,x.EntityId,x.Result,x.CorrelationId,x.IpAddress}));await _audit.RecordAsync("AuditLogExported","AuditLog",safeMetadata:new{Count=rows.Count});return File(bytes,"text/csv",$"audit-logs-{DateTime.UtcNow:yyyyMMdd}.csv");}
 private IQueryable<Models.AuditLog> Query(string? action,string? entityType,DateTime? from,DateTime? to){var q=_db.AuditLogs.AsNoTracking();if(!string.IsNullOrWhiteSpace(action))q=q.Where(x=>x.Action.Contains(action));if(!string.IsNullOrWhiteSpace(entityType))q=q.Where(x=>x.EntityType==entityType);if(from.HasValue)q=q.Where(x=>x.Timestamp>=from.Value);if(to.HasValue)q=q.Where(x=>x.Timestamp<to.Value.Date.AddDays(1));return q;}
}