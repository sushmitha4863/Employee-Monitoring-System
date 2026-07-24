using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalRemarks",
                table: "Leaves",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Leaves",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "Leaves",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "Attendance",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LateMinutes",
                table: "Attendance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ManagerRemarks",
                table: "Attendance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OvertimeHours",
                table: "Attendance",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "WorkSummary",
                table: "Attendance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Leaves_ApprovedById",
                table: "Leaves",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Leaves_Users_ApprovedById",
                table: "Leaves",
                column: "ApprovedById",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Leaves_Users_ApprovedById",
                table: "Leaves");

            migrationBuilder.DropIndex(
                name: "IX_Leaves_ApprovedById",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "ApprovalRemarks",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "ManagerRemarks",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "OvertimeHours",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "WorkSummary",
                table: "Attendance");
        }
    }
}
