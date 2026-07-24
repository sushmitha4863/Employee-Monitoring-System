using EmployeeMonitoringAPI.Interfaces;
namespace EmployeeMonitoringAPI.Services;
public sealed class DevelopmentSmsSender : ISmsSender
{
    private readonly IWebHostEnvironment _environment; private readonly ILogger<DevelopmentSmsSender> _logger;
    public DevelopmentSmsSender(IWebHostEnvironment environment, ILogger<DevelopmentSmsSender> logger) { _environment=environment; _logger=logger; }
    public Task SendOtpAsync(string phoneNumber,string otp,string challengeId,CancellationToken cancellationToken=default) { if(!_environment.IsDevelopment()) throw new InvalidOperationException("Development SMS sender cannot run outside Development."); _logger.LogWarning("DEVELOPMENT MFA OTP ChallengeId={ChallengeId} MaskedPhone={MaskedPhone} OTP={Otp}", challengeId, Mask(phoneNumber), otp); return Task.CompletedTask; }
    private static string Mask(string value)=>value.Length<=4?"****":new string('*',value.Length-4)+value[^4..];
}
public sealed class UnconfiguredProductionSmsSender : ISmsSender { public Task SendOtpAsync(string phoneNumber,string otp,string challengeId,CancellationToken cancellationToken=default)=>throw new InvalidOperationException("A production SMS provider is not configured."); }