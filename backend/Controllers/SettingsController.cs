using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Interfaces;
using EmployeeMonitoringAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EmployeeMonitoringAPI.Controllers;
[Route("api/[controller]")][ApiController][Authorize(Roles="Admin")]
public sealed class SettingsController:ControllerBase
{
 private static readonly Dictionary<string,(string Category,Func<string,bool> Valid)> Allowed=new(StringComparer.OrdinalIgnoreCase){
  ["CompanyName"]=("Company",v=>!string.IsNullOrWhiteSpace(v)&&v.Length<=150),["ApplicationName"]=("Company",v=>!string.IsNullOrWhiteSpace(v)&&v.Length<=150),["CompanyTimezone"]=("Company",v=>TimeZoneInfo.GetSystemTimeZones().Any(z=>z.Id==v)||v=="Asia/Kolkata"),["WorkingDays"]=("Working Hours",v=>!string.IsNullOrWhiteSpace(v)),["StandardWorkHours"]=("Working Hours",v=>decimal.TryParse(v,out var x)&&x>0&&x<=24),["AttendanceLateAfter"]=("Attendance Rules",v=>TimeSpan.TryParse(v,out _)),["SessionTimeoutMinutes"]=("Security",v=>Int(v,5,1440)),["AccountLockoutThreshold"]=("Security",v=>Int(v,3,20)),["MinimumPasswordLength"]=("Password Policy",v=>Int(v,8,128)),["MfaEnabled"]=("Security",Bool),["MfaRequiredForAdmin"]=("Security",Bool),["MfaRequiredForHR"]=("Security",Bool),["MfaRequiredForManager"]=("Security",Bool),["MfaRequiredForEmployee"]=("Security",Bool),["OtpLength"]=("Security",v=>Int(v,6,8)),["OtpExpiryMinutes"]=("Security",v=>Int(v,1,30)),["MaximumOtpAttempts"]=("Security",v=>Int(v,1,10)),["OtpResendCooldownSeconds"]=("Security",v=>Int(v,30,600)),["NotificationPreferences"]=("Notifications",v=>v.Length<=500),["SystemMaintenanceInformation"]=("Maintenance",v=>v.Length<=2000)};
 private readonly ApplicationDbContext _db;private readonly IAuditService _audit;public SettingsController(ApplicationDbContext db,IAuditService audit){_db=db;_audit=audit;}
 [HttpGet]public async Task<IActionResult> Get()=>Ok(await _db.SystemSettings.AsNoTracking().OrderBy(x=>x.Category).ThenBy(x=>x.SettingKey).Select(x=>new{x.SettingId,x.SettingKey,x.SettingValue,x.Category,x.UpdatedAt}).ToListAsync());
 [HttpPut]public async Task<IActionResult> Save(List<SystemSetting> settings){foreach(var input in settings){if(!Allowed.TryGetValue(input.SettingKey,out var rule))return BadRequest(new{message=$"Setting '{input.SettingKey}' is not configurable"});if(!rule.Valid(input.SettingValue??string.Empty))return BadRequest(new{message=$"Setting '{input.SettingKey}' has an invalid value"});var item=await _db.SystemSettings.FirstOrDefaultAsync(x=>x.SettingKey==input.SettingKey);if(item is null){item=new SystemSetting{SettingKey=input.SettingKey,Category=rule.Category};_db.SystemSettings.Add(item);}item.SettingValue=input.SettingValue??string.Empty;item.Category=rule.Category;item.UpdatedAt=DateTime.UtcNow;}await _db.SaveChangesAsync();await _audit.RecordAsync("SettingsChanged","SystemSetting",safeMetadata:new{Keys=settings.Select(x=>x.SettingKey).ToArray()});return Ok(new{message="Settings saved"});}
 private static bool Bool(string v)=>bool.TryParse(v,out _);private static bool Int(string v,int min,int max)=>int.TryParse(v,out var x)&&x>=min&&x<=max;
}
