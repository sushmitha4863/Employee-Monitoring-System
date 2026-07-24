using System.ComponentModel.DataAnnotations;
namespace EmployeeMonitoringAPI.Models;
public class PayrollRecord { [Key] public int PayrollId { get; set; } public int EmployeeId { get; set; } public decimal BasicSalary { get; set; } public decimal Allowances { get; set; } public decimal Deductions { get; set; } public decimal NetSalary { get; set; } public string PayPeriod { get; set; } = string.Empty; public string Status { get; set; } = "Draft"; }
