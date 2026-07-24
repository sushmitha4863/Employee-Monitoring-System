using EmployeeMonitoringAPI.DTOs;
namespace EmployeeMonitoringAPI.Helpers;
public static class ReportDateRange
{
 public static (DateTime From,DateTime To) Resolve(AdminReportQuery query){if(query.From.HasValue||query.To.HasValue)return((query.From??DateTime.MinValue).Date,query.To.HasValue?query.To.Value.Date.AddDays(1):DateTime.MaxValue);var year=query.Year??DateTime.UtcNow.Year;if(!string.IsNullOrWhiteSpace(query.Quarter)&&query.Quarter.Length==2&&int.TryParse(query.Quarter[1..],out var q)&&q is>=1 and<=4){var from=new DateTime(year,(q-1)*3+1,1,0,0,0,DateTimeKind.Utc);return(from,from.AddMonths(3));}return(new DateTime(year,1,1,0,0,0,DateTimeKind.Utc),new DateTime(year+1,1,1,0,0,0,DateTimeKind.Utc));}
}