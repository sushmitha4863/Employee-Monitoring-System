using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.DTOs;
public abstract class HrPersonWriteDto
{
 [Required,StringLength(100,MinimumLength=2)]public string FullName{get;set;}="";
 [Required,StringLength(30)]public string EmployeeCode{get;set;}="";
 [Required,EmailAddress,StringLength(150)]public string Email{get;set;}="";
 [Required,StringLength(100)]public string Username{get;set;}="";
 [RegularExpression(@"^\+?[0-9]{10,15}$",ErrorMessage="Phone number must contain 10 to 15 digits and may start with +."),StringLength(25)]public string?PhoneNumber{get;set;}
 public int?DepartmentId{get;set;}
 [StringLength(100)]public string?Designation{get;set;}
 public DateTime?JoiningDate{get;set;}
 [StringLength(50)]public string EmploymentType{get;set;}="Full-time";
 [StringLength(100)]public string?Location{get;set;}
 public bool IsActive{get;set;}=true;
}
public sealed class CreateHrEmployeeDto:HrPersonWriteDto{[Required,StringLength(128,MinimumLength=8)]public string Password{get;set;}="";public int?ManagerId{get;set;}}
public sealed class UpdateHrEmployeeDto:HrPersonWriteDto{public int?ManagerId{get;set;}}
public sealed class CreateHrManagerDto:HrPersonWriteDto{[Required,StringLength(128,MinimumLength=8)]public string Password{get;set;}="";}
public sealed class UpdateHrManagerDto:HrPersonWriteDto{}
