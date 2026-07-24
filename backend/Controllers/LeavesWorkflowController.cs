using EmployeeMonitoringAPI.Models;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeMonitoringAPI.Controllers;

public partial class LeavesController
{
 [HttpPut("{id:int}/resubmit")][Authorize(Roles="Employee,Manager,HR")]
 public async Task<IActionResult> Resubmit(int id,LeaveResubmitRequest request)
 {
  var userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var leave=await _context.Leaves.FirstOrDefaultAsync(x=>x.LeaveId==id&&x.UserId==userId);if(leave is null)return NotFound(new{message="Leave not found"});if(leave.Status!="Rejected")return Conflict(new{message="Only rejected leave can be edited and resubmitted"});
  if(request.FromDate.Date<DateTime.Today)return BadRequest(new{message="Cannot apply leave in the past"});if(request.ToDate.Date<request.FromDate.Date)return BadRequest(new{message="To date must be after from date"});if(string.IsNullOrWhiteSpace(request.LeaveType))return BadRequest(new{message="Leave type is required"});if(string.IsNullOrWhiteSpace(request.Reason)||request.Reason.Trim().Length<10)return BadRequest(new{message="Reason must be at least 10 characters"});
  var employee=await _context.Users.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId==userId&&x.IsActive);if(employee?.ManagerId is null||!await _context.Users.AnyAsync(x=>x.UserId==employee.ManagerId&&x.IsActive&&x.Role=="Manager"))return BadRequest(new{message="An active assigned Manager is required"});
  if(await _context.Leaves.AnyAsync(x=>x.LeaveId!=id&&x.UserId==userId&&x.Status!="Rejected"&&x.Status!="Cancelled"&&x.FromDate<=request.ToDate.Date&&x.ToDate>=request.FromDate.Date))return Conflict(new{message="Leave request overlaps with an existing leave"});
  leave.LeaveType=request.LeaveType.Trim();leave.FromDate=request.FromDate.Date;leave.ToDate=request.ToDate.Date;leave.IsHalfDay=request.IsHalfDay;leave.Reason=request.Reason.Trim();var days=await LeaveWorkflow.BusinessDays(_context,leave);if(days<=0)return BadRequest(new{message="The selected range contains no working days"});var balance=await _context.LeaveBalances.FirstOrDefaultAsync(x=>x.UserId==userId);if(!leave.LeaveType.Contains("Loss",StringComparison.OrdinalIgnoreCase)&&balance is not null&&days>balance.RemainingLeaves)return Conflict(new{message="Leave request exceeds available balance"});
  LeaveWorkflow.AddHistory(_context,leave,userId,"Rejected","Pending","Edited and resubmitted");leave.Status="Pending";leave.AppliedAt=DateTime.UtcNow;leave.ApprovedById=null;leave.ApprovedAt=null;leave.ApprovalRemarks=null;leave.ClarificationRequest=null;_context.Notifications.Add(new(){UserId=employee.ManagerId.Value,Title="Leave resubmitted",Message=$"{employee.FullName} resubmitted a {leave.LeaveType} request.",Type="LeaveResubmitted",RelatedUrl="/manager/leaves",DeduplicationKey=$"leave:{id}:resubmitted:{leave.AppliedAt:O}"});await _context.SaveChangesAsync();return Ok(new{message="Leave resubmitted"});
 }
 [HttpPost("{id:int}/cancellation-request")][Authorize(Roles="Employee,Manager,HR")]
 public async Task<IActionResult> RequestCancellation(int id,LeaveCancellationRequest request)
 {
  var userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var leave=await _context.Leaves.Include(x=>x.User).FirstOrDefaultAsync(x=>x.LeaveId==id&&x.UserId==userId);if(leave is null)return NotFound(new{message="Leave not found"});if(leave.Status!="Approved")return Conflict(new{message="Only approved leave requires a cancellation request"});if(leave.CancellationRequestedAt.HasValue)return Conflict(new{message="A cancellation request is already pending"});if(string.IsNullOrWhiteSpace(request.Reason))return BadRequest(new{message="Cancellation reason is required"});if(leave.User?.ManagerId is null)return BadRequest(new{message="An assigned Manager is required"});leave.CancellationRequestedAt=DateTime.UtcNow;leave.CancellationReason=request.Reason.Trim();_context.Notifications.Add(new(){UserId=leave.User.ManagerId.Value,Title="Leave cancellation requested",Message=$"{leave.User.FullName} requested cancellation of approved leave.",Type="LeaveCancellation",RelatedUrl="/manager/leaves",DeduplicationKey=$"leave:{id}:cancellation-request"});await _context.SaveChangesAsync();return Ok(new{message="Cancellation request sent to Manager"});
 }
 [HttpPut("{id:int}/cancellation-decision")][Authorize(Roles="Manager")]
 public async Task<IActionResult> CancellationDecision(int id,LeaveCancellationDecisionRequest request)
 {
  var managerId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var leave=await _context.Leaves.Include(x=>x.User).FirstOrDefaultAsync(x=>x.LeaveId==id);if(leave is null)return NotFound(new{message="Leave not found"});if(leave.User?.ManagerId!=managerId)return Forbid();if(leave.Status!="Approved"||!leave.CancellationRequestedAt.HasValue)return Conflict(new{message="No approved leave cancellation is pending"});if(!request.Approve&&string.IsNullOrWhiteSpace(request.Remarks))return BadRequest(new{message="Remarks are required when rejecting cancellation"});
  if(request.Approve){var balance=await _context.LeaveBalances.FirstOrDefaultAsync(x=>x.UserId==leave.UserId);var days=await LeaveWorkflow.BusinessDays(_context,leave);if(balance is not null&&!leave.LeaveType.Contains("Loss",StringComparison.OrdinalIgnoreCase))balance.UsedLeaves=Math.Max(0,balance.UsedLeaves-days);LeaveWorkflow.AddHistory(_context,leave,managerId,"Approved","Cancelled",request.Remarks??"Cancellation approved");leave.Status="Cancelled";}else LeaveWorkflow.AddHistory(_context,leave,managerId,"Approved","Approved",request.Remarks);leave.CancellationRequestedAt=null;leave.CancellationReason=null;_context.Notifications.Add(new(){UserId=leave.UserId,Title=request.Approve?"Leave cancellation approved":"Leave cancellation rejected",Message=request.Remarks?.Trim()??"Your approved leave was cancelled.",Type="LeaveCancellationDecision",RelatedUrl="/employee/attendance",DeduplicationKey=$"leave:{id}:cancellation:{request.Approve}:{DateTime.UtcNow.Ticks}"});await _context.SaveChangesAsync();return Ok(new{message=request.Approve?"Leave cancelled and balance restored":"Cancellation request rejected"});
 }
 [HttpGet("{id:int}/history")]
 public async Task<IActionResult> History(int id){var userId=int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);var leave=await _context.Leaves.AsNoTracking().Include(x=>x.User).FirstOrDefaultAsync(x=>x.LeaveId==id);if(leave is null)return NotFound(new{message="Leave not found"});if(leave.UserId!=userId&&!User.IsInRole("HR")&&leave.User?.ManagerId!=userId)return Forbid();return Ok(await _context.LeaveHistories.AsNoTracking().Where(x=>x.LeaveId==id).OrderBy(x=>x.ChangedAt).Select(x=>new{x.LeaveHistoryId,x.FromStatus,x.ToStatus,x.Remarks,x.ActorUserId,actor=x.ActorUser!.FullName,x.ChangedAt}).ToListAsync());}
}
public sealed class LeaveResubmitRequest{public string LeaveType{get;set;}="";public DateTime FromDate{get;set;}public DateTime ToDate{get;set;}public bool IsHalfDay{get;set;}public string Reason{get;set;}="";}
public sealed class LeaveCancellationRequest{public string Reason{get;set;}="";}
public sealed class LeaveCancellationDecisionRequest{public bool Approve{get;set;}public string? Remarks{get;set;}}