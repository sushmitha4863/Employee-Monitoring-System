using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class CompleteDynamicHrModule20260715 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedHrId",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmploymentStatus",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkLocation",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedHrId",
                table: "RecruitmentJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosingDate",
                table: "RecruitmentJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "RecruitmentJobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "JobCode",
                table: "RecruitmentJobs",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NumberOfOpenings",
                table: "RecruitmentJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpeningDate",
                table: "RecruitmentJobs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RequiredExperience",
                table: "RecruitmentJobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequiredSkills",
                table: "RecruitmentJobs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ApplicantId",
                table: "OnboardingRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedHrId",
                table: "OnboardingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CandidateName",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "OnboardingRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeCode",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmploymentType",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfficialEmail",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AssignedHrId",
                table: "Interviews",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndTime",
                table: "Interviews",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Feedback",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HrRemarks",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "AssignedHrId",
                table: "Applicants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Experience",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "OffboardingRecords",
                columns: table => new
                {
                    OffboardingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedHrId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ResignationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastWorkingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExitType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NoticePeriodDays = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Checklist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccountDeactivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OffboardingRecords", x => x.OffboardingId);
                    table.ForeignKey(
                        name: "FK_OffboardingRecords_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingPrograms",
                columns: table => new
                {
                    TrainingProgramId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedHrId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Trainer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveryMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingPrograms", x => x.TrainingProgramId);
                });

            migrationBuilder.CreateTable(
                name: "TrainingAssignments",
                columns: table => new
                {
                    TrainingAssignmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingProgramId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingAssignments", x => x.TrainingAssignmentId);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_TrainingPrograms_TrainingProgramId",
                        column: x => x.TrainingProgramId,
                        principalTable: "TrainingPrograms",
                        principalColumn: "TrainingProgramId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
DECLARE @DefaultHrId int = (SELECT TOP 1 UserId FROM Users WHERE Role = 'HR' AND IsActive = 1 ORDER BY UserId);
UPDATE Users SET AssignedHrId = @DefaultHrId WHERE AssignedHrId IS NULL AND Role IN ('Employee','Manager');
UPDATE Users SET EmploymentType = 'Full-time' WHERE EmploymentType = '';
UPDATE Users SET EmploymentStatus = CASE WHEN IsActive = 1 THEN 'Active' ELSE 'Inactive' END WHERE EmploymentStatus = '';
UPDATE RecruitmentJobs SET AssignedHrId = COALESCE(@DefaultHrId, 0), JobCode = CONCAT('JOB-', JobId), OpeningDate = CAST(CreatedAt AS date), NumberOfOpenings = 1 WHERE AssignedHrId = 0;
UPDATE Applicants SET AssignedHrId = COALESCE((SELECT AssignedHrId FROM RecruitmentJobs j WHERE j.JobId = Applicants.JobId), COALESCE(@DefaultHrId, 0)) WHERE AssignedHrId = 0;
UPDATE Interviews SET AssignedHrId = COALESCE((SELECT AssignedHrId FROM Applicants a WHERE a.ApplicantId = Interviews.ApplicantId), COALESCE(@DefaultHrId, 0)), EndTime = DATEADD(hour, 1, ScheduledAt) WHERE AssignedHrId = 0;
UPDATE OnboardingRecords SET AssignedHrId = COALESCE((SELECT AssignedHrId FROM Users u WHERE u.UserId = OnboardingRecords.EmployeeId), COALESCE(@DefaultHrId, 0)), CandidateName = COALESCE((SELECT FullName FROM Users u WHERE u.UserId = OnboardingRecords.EmployeeId), 'Legacy onboarding'), Role = COALESCE((SELECT Role FROM Users u WHERE u.UserId = OnboardingRecords.EmployeeId), 'Employee'), EmploymentType = 'Full-time' WHERE AssignedHrId = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users",
                column: "EmployeeCode",
                unique: true,
                filter: "[EmployeeCode] IS NOT NULL AND [EmployeeCode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentJobs_AssignedHrId_JobCode",
                table: "RecruitmentJobs",
                columns: new[] { "AssignedHrId", "JobCode" },
                unique: true,
                filter: "[JobCode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_OffboardingRecords_UserId",
                table: "OffboardingRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_TrainingProgramId_UserId",
                table: "TrainingAssignments",
                columns: new[] { "TrainingProgramId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingAssignments_UserId",
                table: "TrainingAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingPrograms_AssignedHrId_Code",
                table: "TrainingPrograms",
                columns: new[] { "AssignedHrId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OffboardingRecords");

            migrationBuilder.DropTable(
                name: "TrainingAssignments");

            migrationBuilder.DropTable(
                name: "TrainingPrograms");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeCode",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RecruitmentJobs_AssignedHrId_JobCode",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "AssignedHrId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WorkLocation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedHrId",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "ClosingDate",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "JobCode",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "NumberOfOpenings",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "OpeningDate",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "RequiredExperience",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "RequiredSkills",
                table: "RecruitmentJobs");

            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "AssignedHrId",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "CandidateName",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "EmployeeCode",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "OfficialEmail",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "AssignedHrId",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "HrRemarks",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "AssignedHrId",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Applicants");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);
        }
    }
}
