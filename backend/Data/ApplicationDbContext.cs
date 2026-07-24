using EmployeeMonitoringAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace EmployeeMonitoringAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Existing Tables
        public DbSet<User> Users { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<ProjectAssignment> ProjectAssignments { get; set; }

        public DbSet<ProjectHistory> ProjectHistories { get; set; }

        public DbSet<Leave> Leaves { get; set; }

        public DbSet<LeaveBalance> LeaveBalances { get; set; }

        public DbSet<LeaveHistory> LeaveHistories { get; set; }

        public DbSet<CompanyHoliday> CompanyHolidays { get; set; }

        // New Organization Tables
        public DbSet<Department> Departments { get; set; }

        public DbSet<Team> Teams { get; set; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<MfaChallenge> MfaChallenges { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<WorkTask> WorkTasks { get; set; }

        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        public DbSet<DocumentFile> DocumentFiles { get; set; }

        public DbSet<SystemSetting> SystemSettings { get; set; }

        public DbSet<TimesheetEntry> TimesheetEntries { get; set; }

        public DbSet<EmployeePreference> EmployeePreferences { get; set; }
        public DbSet<WeeklyTimesheet> WeeklyTimesheets { get; set; }
        public DbSet<WeeklyTimesheetLine> WeeklyTimesheetLines { get; set; }
        public DbSet<RecruitmentJob> RecruitmentJobs { get; set; }
        public DbSet<Applicant> Applicants { get; set; }
        public DbSet<RecruitmentCandidate> RecruitmentCandidates { get; set; }
        public DbSet<Interview> Interviews { get; set; }
        public DbSet<RecruitmentStatusHistory> RecruitmentStatusHistories { get; set; }
        public DbSet<OnboardingRecord> OnboardingRecords { get; set; }
        public DbSet<OffboardingRecord> OffboardingRecords { get; set; }
        public DbSet<TrainingProgram> TrainingPrograms { get; set; }
        public DbSet<TrainingAssignment> TrainingAssignments { get; set; }
        public DbSet<PerformanceReview> PerformanceReviews { get; set; }
        public DbSet<EmergencyContact> EmergencyContacts { get; set; }
        public DbSet<PromotionHistory> PromotionHistories { get; set; }
        public DbSet<ProjectDiscussion> ProjectDiscussions { get; set; }
        public DbSet<EmployeeMessage> EmployeeMessages { get; set; }
        public DbSet<PayrollRecord> PayrollRecords { get; set; }
        public DbSet<HrPolicy> HrPolicies { get; set; }
        public DbSet<AttendanceRegularization>
    AttendanceRegularizations
        { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TeamMember -> User
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany()
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TeamMember -> Team
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Department
            modelBuilder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Employees)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // User -> Manager (Self Reference)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Manager)
                .WithMany(u => u.Subordinates)
                .HasForeignKey(u => u.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Team -> Manager
            modelBuilder.Entity<Team>()
                .HasOne(t => t.Manager)
                .WithMany()
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ActivityLog -> User
            modelBuilder.Entity<ActivityLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // AttendanceRegularization relationships: restrict deletes to avoid SQL Server multiple cascade paths
            modelBuilder.Entity<AttendanceRegularization>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceRegularization>()
                .HasOne(x => x.Attendance)
                .WithMany()
                .HasForeignKey(x => x.AttendanceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceRegularization>()
                .HasOne(x => x.ApprovedBy)
                .WithMany()
                .HasForeignKey(x => x.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Project>()
                .HasOne(p => p.Manager)
                .WithMany()
                .HasForeignKey(p => p.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.AssignedBy)
                .WithMany()
                .HasForeignKey(t => t.AssignedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkTask>()
                .HasOne(t => t.Project)
                .WithMany()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DocumentFile>()
                .HasOne(d => d.UploadedBy)
                .WithMany()
                .HasForeignKey(d => d.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            modelBuilder.Entity<TimesheetEntry>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TimesheetEntry>()
                .HasOne(t => t.Project)
                .WithMany()
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TimesheetEntry>()
                .HasOne(t => t.Task)
                .WithMany()
                .HasForeignKey(t => t.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TimesheetEntry>()
                .HasOne(t => t.ApprovedBy)
                .WithMany()
                .HasForeignKey(t => t.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EmployeePreference>()
                .HasIndex(p => p.UserId)
                .IsUnique();
            modelBuilder.Entity<EmployeePreference>().Property(x => x.TaskReminders).HasDefaultValue(true);
            modelBuilder.Entity<EmployeePreference>().Property(x => x.CalendarReminders).HasDefaultValue(true);
            modelBuilder.Entity<EmployeePreference>().Property(x => x.TimesheetReminders).HasDefaultValue(true);

            modelBuilder.Entity<LeaveBalance>().Property(x => x.TotalLeaves).HasPrecision(6, 2);
            modelBuilder.Entity<LeaveBalance>().Property(x => x.UsedLeaves).HasPrecision(6, 2);
            modelBuilder.Entity<LeaveHistory>().HasOne(x => x.Leave).WithMany(x => x.History).HasForeignKey(x => x.LeaveId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LeaveHistory>().HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>().HasIndex(x => new { x.UserId, x.AttendanceDate }).IsUnique();
            modelBuilder.Entity<Notification>().HasIndex(x => x.DeduplicationKey).IsUnique().HasFilter("[DeduplicationKey] IS NOT NULL");
            modelBuilder.Entity<WeeklyTimesheet>().HasIndex(x => new { x.EmployeeId, x.WeekStartDate }).IsUnique();
            modelBuilder.Entity<WeeklyTimesheet>().Property(x => x.TotalHours).HasPrecision(6, 2);
            modelBuilder.Entity<WeeklyTimesheet>().HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WeeklyTimesheet>().HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WeeklyTimesheet>().HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WeeklyTimesheetLine>().HasOne(x => x.WeeklyTimesheet).WithMany(x => x.Lines).HasForeignKey(x => x.WeeklyTimesheetId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WeeklyTimesheetLine>().HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<WeeklyTimesheetLine>().HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.SetNull);
            foreach (var property in modelBuilder.Entity<WeeklyTimesheetLine>().Metadata.GetProperties().Where(x => x.ClrType == typeof(decimal))) { property.SetPrecision(6); property.SetScale(2); } 

            modelBuilder.Entity<PayrollRecord>().Property(x => x.BasicSalary).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollRecord>().Property(x => x.Allowances).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollRecord>().Property(x => x.Deductions).HasPrecision(18, 2);
            modelBuilder.Entity<PayrollRecord>().Property(x => x.NetSalary).HasPrecision(18, 2);
            modelBuilder.Entity<PerformanceReview>().Property(x => x.Rating).HasPrecision(5, 2);
            modelBuilder.Entity<Project>().Property(x => x.Budget).HasPrecision(18, 2);
            modelBuilder.Entity<WorkTask>().Property(x => x.EstimatedHours).HasPrecision(8, 2);
            modelBuilder.Entity<PerformanceReview>().Property(x => x.TrainingScore).HasPrecision(5, 2);
            modelBuilder.Entity<PerformanceReview>().Property(x => x.BehaviorScore).HasPrecision(5, 2);
            modelBuilder.Entity<TrainingAssignment>().Property(x => x.Score).HasPrecision(5, 2);
            modelBuilder.Entity<EmergencyContact>().HasIndex(x => new { x.UserId, x.IsPrimary });
            modelBuilder.Entity<PromotionHistory>().HasIndex(x => new { x.UserId, x.EffectiveDate });
            modelBuilder.Entity<ProjectDiscussion>().HasIndex(x => new { x.ProjectId, x.CreatedAt });
            modelBuilder.Entity<EmployeeMessage>().HasIndex(x => new { x.RecipientId, x.SentAt });
            modelBuilder.Entity<EmergencyContact>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PromotionHistory>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProjectDiscussion>().HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ProjectDiscussion>().HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DocumentFile>().HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<PerformanceReview>().HasIndex(x => new { x.ReviewerId, x.EmployeeId, x.Year, x.Quarter }).IsUnique();
            foreach (var property in new[] { nameof(PerformanceReview.WorkQuality), nameof(PerformanceReview.Timeliness), nameof(PerformanceReview.Collaboration), nameof(PerformanceReview.Communication), nameof(PerformanceReview.TechnicalContribution), nameof(PerformanceReview.ProjectContribution) }) modelBuilder.Entity<PerformanceReview>().Property(property).HasPrecision(3, 1);

            modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<User>().HasIndex(x => x.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(x => x.EmployeeCode).IsUnique().HasFilter("[EmployeeCode] IS NOT NULL AND [EmployeeCode] <> ''");
            modelBuilder.Entity<RecruitmentJob>().HasIndex(x => new { x.AssignedHrId, x.JobCode }).IsUnique().HasFilter("[JobCode] <> ''");
            modelBuilder.Entity<Applicant>().HasIndex(x => new { x.AssignedHrId, x.JobId, x.NormalizedEmail }).IsUnique().HasFilter("[JobId] IS NOT NULL AND [NormalizedEmail] <> ''");
            modelBuilder.Entity<Applicant>().HasIndex(x => new { x.AssignedHrId, x.Status, x.AppliedAt });
            modelBuilder.Entity<RecruitmentCandidate>().HasIndex(x => new { x.AssignedHrId, x.NormalizedEmail }).IsUnique();
            modelBuilder.Entity<Applicant>().HasIndex(x => new { x.JobId, x.CandidateId }).IsUnique().HasFilter("[JobId] IS NOT NULL");
            modelBuilder.Entity<Applicant>().HasOne(x => x.Candidate).WithMany(x => x.JobApplications).HasForeignKey(x => x.CandidateId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Applicant>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Interview>().HasOne(x => x.Applicant).WithMany(x => x.Interviews).HasForeignKey(x => x.ApplicantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Interview>().HasIndex(x => new { x.AssignedHrId, x.ScheduledAt });
            modelBuilder.Entity<RecruitmentStatusHistory>().HasOne(x => x.Applicant).WithMany(x => x.StatusHistory).HasForeignKey(x => x.ApplicantId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<RecruitmentStatusHistory>().HasIndex(x => new { x.AssignedHrId, x.ApplicantId, x.ChangedAt });
            modelBuilder.Entity<OnboardingRecord>().HasIndex(x => x.ApplicantId).IsUnique().HasFilter("[ApplicantId] IS NOT NULL");
            modelBuilder.Entity<TrainingProgram>().HasIndex(x => new { x.AssignedHrId, x.Code }).IsUnique();
            modelBuilder.Entity<TrainingAssignment>().HasIndex(x => new { x.TrainingProgramId, x.UserId }).IsUnique();
            modelBuilder.Entity<TrainingAssignment>().HasOne(x => x.TrainingProgram).WithMany(x => x.Assignments).HasForeignKey(x => x.TrainingProgramId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TrainingAssignment>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<OffboardingRecord>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(x => x.IsPrimaryAdmin)
                .IsUnique()
                .HasFilter("[IsPrimaryAdmin] = 1 AND [IsActive] = 1");

            modelBuilder.Entity<Department>()
                .HasIndex(x => x.DepartmentCode)
                .IsUnique();

            modelBuilder.Entity<Project>()
                .HasIndex(x => x.ProjectCode)
                .IsUnique()
                .HasFilter("[ProjectCode] <> ''");

            modelBuilder.Entity<MfaChallenge>()
                .HasIndex(x => x.ChallengeId)
                .IsUnique();

            modelBuilder.Entity<MfaChallenge>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(x => x.Project)
                .WithMany()
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.SetNull);
            // Notification -> User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}




