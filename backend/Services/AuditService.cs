using System.Security.Claims;
using System.Text.Json;
using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Interfaces;
using EmployeeMonitoringAPI.Models;
namespace EmployeeMonitoringAPI.Services;
public sealed class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db; private readonly IHttpContextAccessor _http;
    public AuditService(ApplicationDbContext db, IHttpContextAccessor http) { _db=db; _http=http; }
    public async Task RecordAsync(string action,string entityType,string? entityId=null,string result="Success",object? safeMetadata=null,int? actorUserId=null,CancellationToken cancellationToken=default)
    {
        var context=_http.HttpContext; if(!actorUserId.HasValue && int.TryParse(context?.User.FindFirstValue(ClaimTypes.NameIdentifier),out var current)) actorUserId=current;
        _db.AuditLogs.Add(new AuditLog { ActorUserId=actorUserId, Action=action, EntityType=entityType, EntityId=entityId, Result=result, SafeMetadata=safeMetadata is null?null:JsonSerializer.Serialize(safeMetadata), CorrelationId=context?.TraceIdentifier, IpAddress=context?.Connection.RemoteIpAddress?.ToString(), UserAgent=context?.Request.Headers.UserAgent.ToString(), Timestamp=DateTime.UtcNow });
        await _db.SaveChangesAsync(cancellationToken);
    }
}