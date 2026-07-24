using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class EmployeePersonalPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CalendarReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TaskReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TimesheetReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalendarReminders",
                table: "EmployeePreferences");

            migrationBuilder.DropColumn(
                name: "TaskReminders",
                table: "EmployeePreferences");

            migrationBuilder.DropColumn(
                name: "TimesheetReminders",
                table: "EmployeePreferences");
        }
    }
}
