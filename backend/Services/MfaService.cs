using System.Security.Cryptography;
using System.Text;
using EmployeeMonitoringAPI.Configuration;
using EmployeeMonitoringAPI.Data;
using EmployeeMonitoringAPI.Interfaces;
using EmployeeMonitoringAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
namespace EmployeeMonitoringAPI.Services;
public sealed class MfaService
{
    private readonly ApplicationDbContext _db; private readonly ISmsSender _sms; private readonly MfaOptions _options;
    public MfaService(ApplicationDbContext db,ISmsSender sms,IOptions<MfaOptions> options) { _db=db; _sms=sms; _options=options.Value; }
    public bool IsRequired(User user)=>user.MfaEnabled || (_options.RequiredForAdmin && user.Role.Equals("Admin",StringComparison.OrdinalIgnoreCase));
    public async Task<bool> IsRequiredAsync(User user,CancellationToken cancellationToken=default)
    {
        if(IsRequired(user)) return true;
        var key=$"MfaRequiredFor{user.Role}";
        var configured=await _db.SystemSettings.AsNoTracking().Where(x=>x.SettingKey==key).Select(x=>x.SettingValue).FirstOrDefaultAsync(cancellationToken);
        return bool.TryParse(configured,out var required)&&required;
    }
    public async Task<MfaChallengeResult> CreateAsync(User user,string? ipAddress,string? userAgent,int resendCount=0,CancellationToken cancellationToken=default)
    {
        if(string.IsNullOrWhiteSpace(user.PhoneNumber)) throw new InvalidOperationException("MFA is required but no registered mobile number is available.");
        ValidateOptions(); var otp=GenerateOtp(); var now=DateTime.UtcNow; var challenge=new MfaChallenge { UserId=user.UserId, ChallengeId=Guid.NewGuid().ToString("N"), OtpHash=Hash(otp), CreatedAt=now, ExpiresAt=now.AddMinutes(_options.ExpiryMinutes), LastSentAt=now, ResendCount=resendCount, IpAddress=ipAddress, UserAgent=Truncate(userAgent,500) };
        _db.MfaChallenges.Add(challenge); await _db.SaveChangesAsync(cancellationToken);
        try { await _sms.SendOtpAsync(user.PhoneNumber,otp,challenge.ChallengeId,cancellationToken); }
        catch(Exception ex) when(ex is not OperationCanceledException) { challenge.InvalidatedAt=DateTime.UtcNow; await _db.SaveChangesAsync(CancellationToken.None); throw new MfaDeliveryException("The verification code could not be delivered.",ex); }
        return new MfaChallengeResult(challenge.ChallengeId,Mask(user.PhoneNumber),challenge.ExpiresAt,_options.ResendCooldownSeconds);
    }
    public async Task<MfaVerificationResult> VerifyAsync(string challengeId,string otp,CancellationToken cancellationToken=default)
    {
        var challenge=await _db.MfaChallenges.Include(x=>x.User).FirstOrDefaultAsync(x=>x.ChallengeId==challengeId,cancellationToken); if(challenge?.User is null||!challenge.User.IsActive) return MfaVerificationResult.Fail("The verification challenge is invalid.");
        if(challenge.IsUsed||challenge.InvalidatedAt.HasValue) return MfaVerificationResult.Fail("The verification challenge is no longer valid.");
        if(challenge.ExpiresAt<=DateTime.UtcNow){challenge.InvalidatedAt=DateTime.UtcNow;await _db.SaveChangesAsync(cancellationToken);return MfaVerificationResult.Fail("The verification code has expired.");}
        if(challenge.FailedAttempts>=_options.MaximumAttempts){challenge.InvalidatedAt=DateTime.UtcNow;await _db.SaveChangesAsync(cancellationToken);return MfaVerificationResult.TooMany();}
        if(!FixedEquals(challenge.OtpHash,Hash(otp))){challenge.FailedAttempts++;if(challenge.FailedAttempts>=_options.MaximumAttempts)challenge.InvalidatedAt=DateTime.UtcNow;await _db.SaveChangesAsync(cancellationToken);return challenge.FailedAttempts>=_options.MaximumAttempts?MfaVerificationResult.TooMany():MfaVerificationResult.Fail("The verification code is invalid.");}
        challenge.IsUsed=true;challenge.UsedAt=DateTime.UtcNow;await _db.SaveChangesAsync(cancellationToken);return MfaVerificationResult.Success(challenge.User);
    }
    public async Task<MfaChallengeResult> ResendAsync(string challengeId,string? ipAddress,string? userAgent,CancellationToken cancellationToken=default)
    {
        var old=await _db.MfaChallenges.Include(x=>x.User).FirstOrDefaultAsync(x=>x.ChallengeId==challengeId,cancellationToken); if(old?.User is null||!old.User.IsActive||old.IsUsed||old.InvalidatedAt.HasValue) throw new InvalidOperationException("The verification challenge is invalid.");
        var remaining=_options.ResendCooldownSeconds-(int)(DateTime.UtcNow-old.LastSentAt).TotalSeconds; if(remaining>0) throw new MfaCooldownException(remaining);
        old.InvalidatedAt=DateTime.UtcNow;await _db.SaveChangesAsync(cancellationToken);return await CreateAsync(old.User,ipAddress,userAgent,old.ResendCount+1,cancellationToken);
    }
    private void ValidateOptions(){if(_options.OtpLength<6||_options.OtpLength>8||_options.ExpiryMinutes<1||_options.MaximumAttempts<1||string.IsNullOrWhiteSpace(_options.HashPepper))throw new InvalidOperationException("MFA configuration is incomplete or unsafe.");}
    private string GenerateOtp(){var max=(int)Math.Pow(10,_options.OtpLength);return RandomNumberGenerator.GetInt32(0,max).ToString($"D{_options.OtpLength}");}
    private string Hash(string otp){using var hmac=new HMACSHA256(Encoding.UTF8.GetBytes(_options.HashPepper));return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(otp)));}
    private static bool FixedEquals(string left,string right){try{return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(left),Convert.FromHexString(right));}catch{return false;}}
    private static string Mask(string value)=>value.Length<=4?"****":new string('*',value.Length-4)+value[^4..]; private static string? Truncate(string? value,int max)=>string.IsNullOrEmpty(value)?value:value.Length<=max?value:value[..max];
}
public sealed record MfaChallengeResult(string ChallengeId,string MaskedMobileNumber,DateTime ExpiresAt,int ResendAfterSeconds);
public sealed record MfaVerificationResult(bool Succeeded,bool TooManyAttempts,User? User,string? Message){public static MfaVerificationResult Success(User user)=>new(true,false,user,null);public static MfaVerificationResult Fail(string message)=>new(false,false,null,message);public static MfaVerificationResult TooMany()=>new(false,true,null,"Too many verification attempts.");}
public sealed class MfaDeliveryException : Exception { public MfaDeliveryException(string message,Exception innerException):base(message,innerException){} }
public sealed class MfaCooldownException : Exception { public int RetryAfterSeconds { get; } public MfaCooldownException(int seconds):base("Please wait before requesting another code."){RetryAfterSeconds=seconds;} }

