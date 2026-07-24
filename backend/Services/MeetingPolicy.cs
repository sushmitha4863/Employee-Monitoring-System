using EmployeeMonitoringAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMonitoringAPI.Services;

public sealed record MeetingPolicyFailure(int StatusCode, string Message);

public static class MeetingPolicy
{
    public static async Task<MeetingPolicyFailure?> ValidateAsync(ApplicationDbContext db, int organizerId, string title, DateTime startUtc, DateTime endUtc, IReadOnlyCollection<int> participantIds, string? excludedGroupId = null, CancellationToken cancellationToken = default)
    {
        if (startUtc <= DateTime.UtcNow) return new(400, "Meeting start time must be in the future");
        if (endUtc <= startUtc) return new(400, "Meeting end time must be after start time");

        var ids = participantIds.Distinct().ToList();
        var exact = await db.CalendarEvents.AsNoTracking()
            .Where(x => x.Status != "Cancelled" && x.CreatedById == organizerId && x.Title == title.Trim() && x.StartTime == startUtc && x.EndTime == endUtc && (excludedGroupId == null || x.MeetingGroupId != excludedGroupId))
            .Select(x => new { x.MeetingGroupId, x.UserId }).ToListAsync(cancellationToken);
        if (exact.GroupBy(x => x.MeetingGroupId).Any(g => g.Select(x => x.UserId).OrderBy(x => x).SequenceEqual(ids.OrderBy(x => x))))
            return new(409, "An identical active meeting already exists");

        var conflict = await db.CalendarEvents.AsNoTracking().AnyAsync(x =>
            x.Status != "Cancelled" && (excludedGroupId == null || x.MeetingGroupId != excludedGroupId) &&
            x.StartTime < endUtc && x.EndTime > startUtc &&
            (x.CreatedById == organizerId || x.UserId == organizerId || ids.Contains(x.CreatedById) || ids.Contains(x.UserId)), cancellationToken);
        if (conflict) return new(409, "The organizer or an invited participant has an overlapping meeting");
        return null;
    }
}