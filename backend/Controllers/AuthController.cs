using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.DTOs;
using EmployeeMonitoringAPI.Interfaces;
using EmployeeMonitoringAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
namespace EmployeeMonitoringAPI.Controllers;
[Route("api/[controller]")][ApiController]
public sealed class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db; private readonly PasswordService _passwords; private readonly JwtService _jwt; private readonly MfaService _mfa; private readonly IAuditService _audit;
    private static readonly string[] SupportedRegistrationRoles = { "Employee", "Manager" };
    public AuthController(ApplicationDbContext db,PasswordService passwords,JwtService jwt,MfaService mfa,IAuditService audit){_db=db;_passwords=passwords;_jwt=jwt;_mfa=mfa;_audit=audit;}

    [HttpPost("register")][Authorize(Roles="HR")]
    public async Task<IActionResult> Register(CreateEmployeeDto request)
    {
        if(request.Role.Equals("Admin",StringComparison.OrdinalIgnoreCase)) return Forbid();
        if(!SupportedRegistrationRoles.Contains(request.Role,StringComparer.OrdinalIgnoreCase)) return BadRequest(new{message="Unsupported role"});
        if(await _db.Users.AnyAsync(x=>x.Email==request.Email||x.Username==request.Username)) return Conflict(new{message="User already exists"});
        var user=new EmployeeMonitoringAPI.Models.User{FullName=request.FullName.Trim(),Email=request.Email.Trim(),Username=request.Username.Trim(),Role=request.Role,AssignedHrId=int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value),EmployeeCode=request.EmployeeCode,PhoneNumber=request.PhoneNumber,Designation=request.Designation,DateOfJoining=request.DateOfJoining,DepartmentId=request.DepartmentId,ManagerId=request.ManagerId,PasswordHash=_passwords.HashPassword(request.Password),IsActive=true,CreatedAt=DateTime.UtcNow};
        _db.Users.Add(user);await _db.SaveChangesAsync();await _audit.RecordAsync("UserCreated","User",user.UserId.ToString(),safeMetadata:new{user.Role});return Ok(new{message="User registered successfully",user.UserId});
    }

    [HttpPost("login")][EnableRateLimiting("auth-login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var identifier=request.Username.Trim(); var user=await _db.Users.FirstOrDefaultAsync(x=>x.Username==identifier||x.Email==identifier);
        if(user is null||!user.IsActive||!_passwords.VerifyPassword(user.PasswordHash,request.Password)){await _audit.RecordAsync("LoginFailure","Authentication",result:"Failure");return Unauthorized(new{message="Invalid username or password"});}
        if(await _mfa.IsRequiredAsync(user))
        {
            try{var challenge=await _mfa.CreateAsync(user,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent);await _audit.RecordAsync("MfaChallengeCreated","MfaChallenge",challenge.ChallengeId,actorUserId:user.UserId);return Ok(new{mfaRequired=true,challenge.ChallengeId,challenge.MaskedMobileNumber,challenge.ExpiresAt,challenge.ResendAfterSeconds});}
            catch(MfaDeliveryException){await _audit.RecordAsync("MfaDeliveryFailure","MfaChallenge",result:"Failure",actorUserId:user.UserId);return StatusCode(StatusCodes.Status503ServiceUnavailable,new{message="The verification code could not be delivered. Please try again."});}
            catch(InvalidOperationException){await _audit.RecordAsync("MfaDeliveryFailure","MfaChallenge",result:"Failure",actorUserId:user.UserId);return StatusCode(StatusCodes.Status503ServiceUnavailable,new{message="Multi-factor authentication is temporarily unavailable."});}
        }
        user.LastLoginAt=DateTime.UtcNow;await _db.SaveChangesAsync();await _audit.RecordAsync("LoginSuccess","Authentication",actorUserId:user.UserId);return Ok(LoginResponse(user,false));
    }

    [HttpPost("mfa/verify")][EnableRateLimiting("mfa-verify")]
    public async Task<IActionResult> Verify(MfaVerifyRequest request)
    {
        var result=await _mfa.VerifyAsync(request.ChallengeId,request.Otp);if(!result.Succeeded){await _audit.RecordAsync("MfaVerificationFailure","MfaChallenge",request.ChallengeId,result:"Failure");if(result.TooManyAttempts)return StatusCode(StatusCodes.Status429TooManyRequests,new{message=result.Message});return BadRequest(new{message=result.Message});}
        result.User!.LastLoginAt=DateTime.UtcNow;await _db.SaveChangesAsync();await _audit.RecordAsync("MfaVerificationSuccess","MfaChallenge",request.ChallengeId,actorUserId:result.User.UserId);return Ok(LoginResponse(result.User,true));
    }

    [HttpPost("mfa/resend")][EnableRateLimiting("mfa-resend")]
    public async Task<IActionResult> Resend(MfaResendRequest request)
    {
        try{var challenge=await _mfa.ResendAsync(request.ChallengeId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent);await _audit.RecordAsync("MfaCodeResent","MfaChallenge",challenge.ChallengeId);return Ok(new{challenge.ChallengeId,challenge.MaskedMobileNumber,challenge.ExpiresAt,challenge.ResendAfterSeconds});}
        catch(MfaCooldownException ex){Response.Headers.RetryAfter=ex.RetryAfterSeconds.ToString();return StatusCode(StatusCodes.Status429TooManyRequests,new{message=ex.Message,retryAfterSeconds=ex.RetryAfterSeconds});}
        catch(MfaDeliveryException){await _audit.RecordAsync("MfaDeliveryFailure","MfaChallenge",request.ChallengeId,result:"Failure");return StatusCode(StatusCodes.Status503ServiceUnavailable,new{message="The new verification code could not be delivered. Please try again."});}
        catch(InvalidOperationException){return BadRequest(new{message="The verification challenge is invalid."});}
    }

    private object LoginResponse(EmployeeMonitoringAPI.Models.User user,bool mfaEnabled)=>new{token=_jwt.GenerateToken(user),user.UserId,user.FullName,user.Email,user.Username,user.Role,mfaEnabled};
}
