using System.ComponentModel.DataAnnotations;

namespace EmployeeMonitoringAPI.DTOs;

public sealed class CreateHrUserDto
{
    [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Required, StringLength(30)] public string EmployeeCode { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Phone, StringLength(25)] public string? PhoneNumber { get; set; }
    [Required] public int DepartmentId { get; set; }
    [Required, StringLength(100)] public string Designation { get; set; } = string.Empty;
    [Required, StringLength(128, MinimumLength = 8)] public string Password { get; set; } = string.Empty;
    [Required] public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class UpdateHrUserDto
{
    [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = string.Empty;
    [Required, StringLength(30)] public string EmployeeCode { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(150)] public string Email { get; set; } = string.Empty;
    [Phone, StringLength(25)] public string? PhoneNumber { get; set; }
    [Required] public int DepartmentId { get; set; }
    [Required, StringLength(100)] public string Designation { get; set; } = string.Empty;
}

public sealed class AdminResetPasswordDto
{
    [Required, StringLength(128, MinimumLength = 8)] public string NewPassword { get; set; } = string.Empty;
    [Required] public string ConfirmPassword { get; set; } = string.Empty;
}