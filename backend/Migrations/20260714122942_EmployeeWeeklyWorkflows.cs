using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeWeeklyWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendance_UserId",
                table: "Attendance");

            migrationBuilder.AddColumn<string>(
                name: "ManagerFeedback",
                table: "WorkTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "WorkTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "WorkTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "Notifications",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WeeklyTimesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WeekEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedById = table.Column<int>(type: "int", nullable: true),
                    ManagerRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyTimesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyTimesheets_Users_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyTimesheets_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyTimesheets_Users_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyTimesheetLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WeeklyTimesheetId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    WorkDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BillingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MondayHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    TuesdayHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    WednesdayHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    ThursdayHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    FridayHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyTimesheetLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyTimesheetLines_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyTimesheetLines_WeeklyTimesheets_WeeklyTimesheetId",
                        column: x => x.WeeklyTimesheetId,
                        principalTable: "WeeklyTimesheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WeeklyTimesheetLines_WorkTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeduplicationKey",
                table: "Notifications",
                column: "DeduplicationKey",
                unique: true,
                filter: "[DeduplicationKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UserId_AttendanceDate",
                table: "Attendance",
                columns: new[] { "UserId", "AttendanceDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimesheetLines_ProjectId",
                table: "WeeklyTimesheetLines",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimesheetLines_TaskId",
                table: "WeeklyTimesheetLines",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimesheetLines_WeeklyTimesheetId",
                table: "WeeklyTimesheetLines",
                column: "WeeklyTimesheetId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimesheets_EmployeeId_WeekStartDate",
                table: "WeeklyTimesheets",
                columns: new[] { "EmployeeId", "WeekStartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimesheets_ManagerId",
                table: "WeeklyTimesheets",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyTimesheets_ReviewedById",
                table: "WeeklyTimesheets",
                column: "ReviewedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeeklyTimesheetLines");

            migrationBuilder.DropTable(
                name: "WeeklyTimesheets");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_DeduplicationKey",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_UserId_AttendanceDate",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "ManagerFeedback",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "Notifications");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UserId",
                table: "Attendance",
                column: "UserId");
        }
    }
}
