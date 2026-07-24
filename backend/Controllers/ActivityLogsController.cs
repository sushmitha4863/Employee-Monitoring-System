using EmployeeMonitoringAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EmployeeMonitoringAPI.Controllers;
[Route("api/[controller]")][ApiController][Authorize(Roles="Admin")]
public sealed class ActivityLogsController:ControllerBase{private readonly ApplicationDbContext _db;public ActivityLogsController(ApplicationDbContext db){_db=db;}[HttpGet]public async Task<IActionResult> Get()=>Ok(await _db.ActivityLogs.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(100).Select(x=>new{x.Id,x.UserId,x.Action,x.CreatedAt}).ToListAsync());}