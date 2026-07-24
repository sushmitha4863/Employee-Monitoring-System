using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("LeaveBalances")]
    public class LeaveBalance
    {
        [Key]
        public int BalanceId { get; set; }

        public int UserId { get; set; }

        public decimal TotalLeaves { get; set; } = 12;

        public decimal UsedLeaves { get; set; }

        public decimal RemainingLeaves => TotalLeaves - UsedLeaves;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}