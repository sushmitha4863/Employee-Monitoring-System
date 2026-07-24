using System.Net;
using System.Text.Json;
namespace EmployeeMonitoringAPI.Helpers;
public sealed class ExceptionMiddleware
{
 private readonly RequestDelegate _next;private readonly ILogger<ExceptionMiddleware> _logger;private readonly IWebHostEnvironment _environment;public ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger,IWebHostEnvironment environment){_next=next;_logger=logger;_environment=environment;}
 public async Task InvokeAsync(HttpContext context){try{await _next(context);}catch(ArgumentException ex){await Write(context,HttpStatusCode.BadRequest,ex.Message);}catch(Exception ex){_logger.LogError(ex,"Unhandled exception. CorrelationId {CorrelationId}",context.TraceIdentifier);await Write(context,HttpStatusCode.InternalServerError,"An unexpected error occurred. Use the correlation ID when contacting support.");}}
 private static Task Write(HttpContext context,HttpStatusCode status,string message){context.Response.ContentType="application/json";context.Response.StatusCode=(int)status;return context.Response.WriteAsync(JsonSerializer.Serialize(new{success=false,message,correlationId=context.TraceIdentifier}));}
}