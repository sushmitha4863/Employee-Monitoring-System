using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class CompleteRecruitmentWorkflow20260715 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MeetingUrl",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Round",
                table: "Interviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Applicants",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "ExperienceLevel",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "Applicants",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Qualification",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RecruitmentStatusHistories",
                columns: table => new
                {
                    RecruitmentStatusHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    AssignedHrId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedById = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentStatusHistories", x => x.RecruitmentStatusHistoryId);
                    table.ForeignKey(
                        name: "FK_RecruitmentStatusHistories_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "ApplicantId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OnboardingRecords_ApplicantId",
                table: "OnboardingRecords",
                column: "ApplicantId",
                unique: true,
                filter: "[ApplicantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_ApplicantId",
                table: "Interviews",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_AssignedHrId_ScheduledAt",
                table: "Interviews",
                columns: new[] { "AssignedHrId", "ScheduledAt" });

            migrationBuilder.Sql(@"
                UPDATE Applicants SET NormalizedEmail = UPPER(LTRIM(RTRIM(Email))) WHERE ISNULL(Email, '') <> '';
                WITH duplicates AS (
                    SELECT ApplicantId, ROW_NUMBER() OVER (PARTITION BY AssignedHrId, JobId, NormalizedEmail ORDER BY ApplicantId) AS rn
                    FROM Applicants WHERE JobId IS NOT NULL AND NormalizedEmail <> ''
                )
                UPDATE a SET NormalizedEmail = CONCAT(a.NormalizedEmail, '#LEGACY-', a.ApplicantId)
                FROM Applicants a INNER JOIN duplicates d ON d.ApplicantId = a.ApplicantId WHERE d.rn > 1;");
            migrationBuilder.CreateIndex(
                name: "IX_Applicants_AssignedHrId_JobId_NormalizedEmail",
                table: "Applicants",
                columns: new[] { "AssignedHrId", "JobId", "NormalizedEmail" },
                unique: true,
                filter: "[JobId] IS NOT NULL AND [NormalizedEmail] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_AssignedHrId_Status_AppliedAt",
                table: "Applicants",
                columns: new[] { "AssignedHrId", "Status", "AppliedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_JobId",
                table: "Applicants",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentStatusHistories_ApplicantId",
                table: "RecruitmentStatusHistories",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentStatusHistories_AssignedHrId_ApplicantId_ChangedAt",
                table: "RecruitmentStatusHistories",
                columns: new[] { "AssignedHrId", "ApplicantId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Applicants_RecruitmentJobs_JobId",
                table: "Applicants",
                column: "JobId",
                principalTable: "RecruitmentJobs",
                principalColumn: "JobId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Interviews_Applicants_ApplicantId",
                table: "Interviews",
                column: "ApplicantId",
                principalTable: "Applicants",
                principalColumn: "ApplicantId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applicants_RecruitmentJobs_JobId",
                table: "Applicants");

            migrationBuilder.DropForeignKey(
                name: "FK_Interviews_Applicants_ApplicantId",
                table: "Interviews");

            migrationBuilder.DropTable(
                name: "RecruitmentStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_OnboardingRecords_ApplicantId",
                table: "OnboardingRecords");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_ApplicantId",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_Interviews_AssignedHrId_ScheduledAt",
                table: "Interviews");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_AssignedHrId_JobId_NormalizedEmail",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_AssignedHrId_Status_AppliedAt",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_JobId",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "MeetingUrl",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "Round",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "ExperienceLevel",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "Qualification",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "Applicants");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
