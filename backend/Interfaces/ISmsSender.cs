namespace EmployeeMonitoringAPI.Interfaces;
public interface ISmsSender { Task SendOtpAsync(string phoneNumber, string otp, string challengeId, CancellationToken cancellationToken = default); }