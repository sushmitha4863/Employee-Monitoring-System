using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace EmployeeMonitoringAPI.Controllers;
[Route("api/admin-ownership")][ApiController][Authorize(Roles="Admin")]
public sealed class AdminOwnershipController:ControllerBase
{
 private readonly ApplicationDbContext _db;private readonly IAuditService _audit;public AdminOwnershipController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
 [HttpPost("transfer/{newPrimaryAdminId:int}")]
 public async Task<IActionResult> Transfer(int newPrimaryAdminId){var currentId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);await using var tx=await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);var current=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==currentId&&x.Role=="Admin"&&x.IsPrimaryAdmin&&x.IsActive);if(current is null)return Forbid();var next=await _db.Users.FirstOrDefaultAsync(x=>x.UserId==newPrimaryAdminId&&x.Role=="Admin"&&x.IsActive);if(next is null)return BadRequest(new{message="The replacement must be an active Admin"});if(next.UserId==current.UserId)return Conflict(new{message="This account is already the Primary Admin"});current.IsPrimaryAdmin=false;next.IsPrimaryAdmin=true;await _db.SaveChangesAsync();await tx.CommitAsync();await _audit.RecordAsync("PrimaryAdminTransferred","User",next.UserId.ToString(),safeMetadata:new{PreviousPrimaryAdminId=current.UserId});return Ok(new{message="Primary Admin ownership transferred"});}
}