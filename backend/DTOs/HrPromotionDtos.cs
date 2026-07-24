using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.DTOs;
public sealed class HrPromotionDecisionDto{[Required]public int UserId{get;set;}[Range(2000,2200)]public int Year{get;set;}[Required,RegularExpression("Q[1-4]")]public string Quarter{get;set;}="";[Required,RegularExpression("Approved|Rejected")]public string Decision{get;set;}="";public DateTime?EffectiveDate{get;set;}[StringLength(2000)]public string?Remarks{get;set;}}
