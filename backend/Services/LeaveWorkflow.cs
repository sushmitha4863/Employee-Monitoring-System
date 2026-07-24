using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMonitoringAPI.Services;

public sealed record LeaveTransitionFailure(int StatusCode, string Message);

public static class LeaveWorkflow
{
 public static async Task<decimal> BusinessDays(ApplicationDbContext db, Leave leave, CancellationToken token=default)
 {
  if(leave.IsHalfDay)return 0.5m;
  var holidays=await db.CompanyHolidays.Where(x=>x.IsActive&&x.HolidayDate>=leave.FromDate.Date&&x.HolidayDate<=leave.ToDate.Date).Select(x=>x.HolidayDate.Date).ToListAsync(token);
  return Enumerable.Range(0,(leave.ToDate.Date-leave.FromDate.Date).Days+1).Select(i=>leave.FromDate.Date.AddDays(i)).Count(x=>x.DayOfWeek is not(DayOfWeek.Saturday or DayOfWeek.Sunday)&&!holidays.Contains(x));
 }
 public static async Task<LeaveTransitionFailure?> ReviewAsync(ApplicationDbContext db,Leave leave,int actorId,string decision,string? remarks,CancellationToken token=default)
 {
  if(leave.Status!="Pending")return new(409,"Only pending leave requests can be reviewed");
  if(decision is not("Approved" or "Rejected"))return new(400,"Decision must be Approved or Rejected");
  if(decision=="Rejected"&&string.IsNullOrWhiteSpace(remarks))return new(400,"Manager remarks are required when rejecting leave");
  if(decision=="Approved")
  {
   var balance=await db.LeaveBalances.FirstOrDefaultAsync(x=>x.UserId==leave.UserId,token)??new LeaveBalance{UserId=leave.UserId,TotalLeaves=12};if(balance.BalanceId==0)db.LeaveBalances.Add(balance);
   var days=await BusinessDays(db,leave,token);if(!leave.LeaveType.Contains("Loss",StringComparison.OrdinalIgnoreCase)&&days>balance.RemainingLeaves)return new(409,"Leave balance is insufficient for approval");if(!leave.LeaveType.Contains("Loss",StringComparison.OrdinalIgnoreCase))balance.UsedLeaves+=days;
  }
  db.LeaveHistories.Add(new(){LeaveId=leave.LeaveId,FromStatus=leave.Status,ToStatus=decision,Remarks=remarks?.Trim(),ActorUserId=actorId});leave.Status=decision;leave.ApprovedById=actorId;leave.ApprovedAt=DateTime.UtcNow;leave.ApprovalRemarks=remarks?.Trim();return null;
 }
 public static void AddHistory(ApplicationDbContext db,Leave leave,int actorId,string from,string to,string? remarks)=>db.LeaveHistories.Add(new(){LeaveId=leave.LeaveId,FromStatus=from,ToStatus=to,Remarks=remarks?.Trim(),ActorUserId=actorId});
}