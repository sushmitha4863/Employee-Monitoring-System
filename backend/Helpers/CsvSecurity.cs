using System.Text;
namespace EmployeeMonitoringAPI.Helpers;
public static class CsvSecurity
{
 public static string Protect(string? value){value??=string.Empty;if(value.Length>0&&"=+-@".Contains(value[0]))value="'"+value;return value;}
 public static string Escape(string? value){var safe=Protect(value);return safe.ContainsAny(',', '"', '\r', '\n')?"\""+safe.Replace("\"","\"\"")+"\"":safe;}
 private static bool ContainsAny(this string value,params char[] chars)=>value.IndexOfAny(chars)>=0;
 public static byte[] Build(IEnumerable<string> headers,IEnumerable<IEnumerable<string?>> rows){var sb=new StringBuilder();sb.AppendLine(string.Join(',',headers.Select(Escape)));foreach(var row in rows)sb.AppendLine(string.Join(',',row.Select(Escape)));return new UTF8Encoding(true).GetBytes(sb.ToString());}
}