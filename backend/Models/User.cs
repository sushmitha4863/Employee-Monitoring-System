using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeMonitoringAPI.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [NotMapped]
        public string? Password { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Employee";

        public bool IsActive { get; set; } = true;

        public string? ProfileImage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? DepartmentId { get; set; }

        public Department? Department { get; set; }

        public int? AssignedHrId { get; set; }

        public int? ManagerId { get; set; }

        public User? Manager { get; set; }

        public ICollection<User>? Subordinates { get; set; }
        public string? EmployeeCode { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Designation { get; set; }

        public string EmploymentType { get; set; } = "Full-time";
        public string EmploymentStatus { get; set; } = "Active";
        public string? WorkLocation { get; set; }

        public DateTime? DateOfJoining { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? Address { get; set; }
        public bool IsPrimaryAdmin { get; set; }
        public bool MfaEnabled { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime? LastPasswordChangedAt { get; set; }
        public int FailedPasswordChangeAttempts { get; set; }
        public DateTime? PasswordChangeLockedUntil { get; set; }
    }
}
