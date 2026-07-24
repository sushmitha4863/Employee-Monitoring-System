using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class EmployeeReminderPreferenceDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [EmployeePreferences] SET [TaskReminders] = 1, [CalendarReminders] = 1, [TimesheetReminders] = 1");
            migrationBuilder.AlterColumn<bool>(
                name: "TimesheetReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "TaskReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "CalendarReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [EmployeePreferences] SET [TaskReminders] = 1, [CalendarReminders] = 1, [TimesheetReminders] = 1");
            migrationBuilder.AlterColumn<bool>(
                name: "TimesheetReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "TaskReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<bool>(
                name: "CalendarReminders",
                table: "EmployeePreferences",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);
        }
    }
}
