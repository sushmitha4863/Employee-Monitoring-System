using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class CompleteEssModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmployeeRemarks",
                table: "WorkTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "WorkTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakEndTime",
                table: "Attendance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BreakMinutes",
                table: "Attendance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BreakStartTime",
                table: "Attendance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "EffectiveHours",
                table: "Attendance",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "EmployeePreferences",
                columns: table => new
                {
                    EmployeePreferenceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Theme = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultLandingPage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    CalendarVisibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timezone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePreferences", x => x.EmployeePreferenceId);
                    table.ForeignKey(
                        name: "FK_EmployeePreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimesheetEntries",
                columns: table => new
                {
                    TimesheetEntryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: true),
                    WorkDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Hours = table.Column<double>(type: "float", nullable: false),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false),
                    WorkDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DailyNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ManagerComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetEntries", x => x.TimesheetEntryId);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "ProjectId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetEntries_WorkTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "WorkTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePreferences_UserId",
                table: "EmployeePreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_ApprovedById",
                table: "TimesheetEntries",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_ProjectId",
                table: "TimesheetEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_TaskId",
                table: "TimesheetEntries",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetEntries_UserId",
                table: "TimesheetEntries",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeePreferences");

            migrationBuilder.DropTable(
                name: "TimesheetEntries");

            migrationBuilder.DropColumn(
                name: "EmployeeRemarks",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "BreakEndTime",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "BreakMinutes",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "BreakStartTime",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "EffectiveHours",
                table: "Attendance");
        }
    }
}
