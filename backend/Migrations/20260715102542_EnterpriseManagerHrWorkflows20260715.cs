using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseManagerHrWorkflows20260715 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedHours",
                table: "WorkTasks",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertificateUrl",
                table: "TrainingAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Score",
                table: "TrainingAssignments",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Budget",
                table: "Projects",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Client",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BehaviorScore",
                table: "PerformanceReviews",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Goals",
                table: "PerformanceReviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Kpis",
                table: "PerformanceReviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PromotionRecommendation",
                table: "PerformanceReviews",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TrainingScore",
                table: "PerformanceReviews",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AccountsStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DocumentsStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmailStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EquipmentStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrientationStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "OnboardingRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TrainingStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VerificationStage",
                table: "OnboardingRecords",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentUrl",
                table: "Leaves",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClarificationRequest",
                table: "Leaves",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "DocumentFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferLetterUrl",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OfferStatus",
                table: "Applicants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferedAt",
                table: "Applicants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmergencyContacts",
                columns: table => new
                {
                    EmergencyContactId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyContacts", x => x.EmergencyContactId);
                    table.ForeignKey(
                        name: "FK_EmergencyContacts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeMessages",
                columns: table => new
                {
                    EmployeeMessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    RecipientId = table.Column<int>(type: "int", nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeMessages", x => x.EmployeeMessageId);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDiscussions",
                columns: table => new
                {
                    ProjectDiscussionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    AuthorId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDiscussions", x => x.ProjectDiscussionId);
                    table.ForeignKey(
                        name: "FK_ProjectDiscussions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectDiscussions_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PromotionHistories",
                columns: table => new
                {
                    PromotionHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FromDesignation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToDesignation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionHistories", x => x.PromotionHistoryId);
                    table.ForeignKey(
                        name: "FK_PromotionHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentFiles_ProjectId",
                table: "DocumentFiles",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyContacts_UserId_IsPrimary",
                table: "EmergencyContacts",
                columns: new[] { "UserId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeMessages_RecipientId_SentAt",
                table: "EmployeeMessages",
                columns: new[] { "RecipientId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDiscussions_AuthorId",
                table: "ProjectDiscussions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDiscussions_ProjectId_CreatedAt",
                table: "ProjectDiscussions",
                columns: new[] { "ProjectId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionHistories_UserId_EffectiveDate",
                table: "PromotionHistories",
                columns: new[] { "UserId", "EffectiveDate" });

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentFiles_Projects_ProjectId",
                table: "DocumentFiles",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.SetNull);
            migrationBuilder.Sql(@"
                UPDATE OnboardingRecords SET DocumentsStage='Pending', VerificationStage='Pending', EquipmentStage='Pending', EmailStage='Pending', AccountsStage='Pending', TrainingStage='Pending', OrientationStage='Pending' WHERE DocumentsStage='';
                UPDATE PerformanceReviews SET PromotionRecommendation='Not Recommended' WHERE PromotionRecommendation='';
                MERGE SystemSettings AS target
                USING (VALUES
                  ('TaskStatuses','Not Started,Assigned,In Progress,Blocked,Under Review,Completed,Cancelled','Workflow'),
                  ('TaskPriorities','Low,Medium,High,Critical','Workflow'),
                  ('ProjectStatuses','Queued,Ongoing,Review,Completed,Delayed,At Risk,On Hold,Cancelled','Workflow'),
                  ('ProjectPriorities','Low,Medium,High,Critical','Workflow'),
                  ('AttendanceStatuses','Present,Absent,Late,WFH,Half Day,Missing Checkout','Workflow'),
                  ('LeaveStatuses','Pending,Clarification Requested,Approved,Rejected,Cancelled','Workflow'),
                  ('TimesheetStatuses','Draft,Submitted,Changes Requested,Approved,Rejected','Workflow'),
                  ('PerformanceRecommendations','Not Recommended,Recommended,Strongly Recommended','Workflow'),
                  ('OnboardingStages','Documents,Verification,Equipment,Email,Accounts,Training,Orientation','Workflow'),
                  ('OnboardingStageStatuses','Pending,In Progress,Blocked,Completed','Workflow'),
                  ('TrainingStatuses','Assigned,In Progress,Completed,Failed,Cancelled','Workflow'),
                  ('EmploymentStatuses','Active,Probation,Notice Period,On Leave,Inactive,Offboarded','Workflow'),
                  ('ReportTypes','attendance,leave,recruitment,performance,training,department,project,manager,employee','Workflow')
                ) AS source(SettingKey,SettingValue,Category)
                ON target.SettingKey=source.SettingKey
                WHEN MATCHED THEN UPDATE SET SettingValue=source.SettingValue,Category=source.Category,UpdatedAt=GETUTCDATE()
                WHEN NOT MATCHED THEN INSERT(SettingKey,SettingValue,Category,UpdatedAt) VALUES(source.SettingKey,source.SettingValue,source.Category,GETUTCDATE());");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentFiles_Projects_ProjectId",
                table: "DocumentFiles");

            migrationBuilder.DropTable(
                name: "EmergencyContacts");

            migrationBuilder.DropTable(
                name: "EmployeeMessages");

            migrationBuilder.DropTable(
                name: "ProjectDiscussions");

            migrationBuilder.DropTable(
                name: "PromotionHistories");

            migrationBuilder.DropIndex(
                name: "IX_DocumentFiles_ProjectId",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "EstimatedHours",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CertificateUrl",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "TrainingAssignments");

            migrationBuilder.DropColumn(
                name: "Budget",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Client",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BehaviorScore",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Goals",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Kpis",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "PromotionRecommendation",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "TrainingScore",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "AccountsStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "DocumentsStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "EmailStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "EquipmentStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "OrientationStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "TrainingStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "VerificationStage",
                table: "OnboardingRecords");

            migrationBuilder.DropColumn(
                name: "AttachmentUrl",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "ClarificationRequest",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "DocumentFiles");

            migrationBuilder.DropColumn(
                name: "OfferLetterUrl",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "OfferStatus",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "OfferedAt",
                table: "Applicants");
        }
    }
}
