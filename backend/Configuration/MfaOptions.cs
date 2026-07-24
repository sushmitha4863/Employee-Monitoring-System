namespace EmployeeMonitoringAPI.Configuration;
public sealed class MfaOptions
{
    public const string SectionName = "Mfa";
    public bool RequiredForAdmin { get; set; } = true;
    public int OtpLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 5;
    public int MaximumAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 60;
    public string HashPepper { get; set; } = string.Empty;
    public string SmsProvider { get; set; } = "Development";
}