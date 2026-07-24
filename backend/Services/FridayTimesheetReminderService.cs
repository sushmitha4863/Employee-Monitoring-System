using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMonitoringAPI.Services;

public sealed class FridayTimesheetReminderService(IServiceScopeFactory scopeFactory, ILogger<FridayTimesheetReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnce(DateTimeOffset.UtcNow, stoppingToken); } catch (Exception ex) { logger.LogError(ex, "Friday timesheet reminder run failed"); }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    public async Task<int> RunOnce(DateTimeOffset utcNow, CancellationToken token = default)
    {
        using var scope = scopeFactory.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var zoneName = await db.SystemSettings.Where(x => x.SettingKey == "CompanyTimezone").Select(x => x.SettingValue).FirstOrDefaultAsync(token) ?? "India Standard Time";
        TimeZoneInfo zone; try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneName); } catch { zone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        var local = TimeZoneInfo.ConvertTime(utcNow, zone); if (local.DayOfWeek != DayOfWeek.Friday) return 0;
        var week = local.Date.AddDays(-4); var employees = await db.Users.Where(x => x.IsActive && x.Role == "Employee").Select(x => x.UserId).ToListAsync(token); var created = 0;
        foreach (var id in employees)
        {
            var key = $"timesheet-friday:{id}:{week:yyyyMMdd}";
            if (await db.EmployeePreferences.AnyAsync(x => x.UserId == id && !x.TimesheetReminders, token) || await db.WeeklyTimesheets.AnyAsync(x => x.EmployeeId == id && x.WeekStartDate == week && (x.Status == "Submitted" || x.Status == "Approved"), token) || await db.Notifications.AnyAsync(x => x.DeduplicationKey == key, token)) continue;
            db.Notifications.Add(new Notification { UserId = id, Title = "Timesheet reminder", Message = "Please complete and submit your Timesheet for this week.", Type = "TimesheetReminder", RelatedUrl = "/employee/timesheet", DeduplicationKey = key, CreatedAt = DateTime.UtcNow }); created++;
        }
        if (created > 0) await db.SaveChangesAsync(token); return created;
    }
}
