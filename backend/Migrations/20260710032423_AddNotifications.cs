using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceRegularizations",
                columns: table => new
                {
                    RequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AttendanceId = table.Column<int>(type: "int", nullable: false),
                    RequestedCheckIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedCheckOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedById = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManagerRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRegularizations", x => x.RequestId);
                    table.ForeignKey(
                        name: "FK_AttendanceRegularizations_Attendance_AttendanceId",
                        column: x => x.AttendanceId,
                        principalTable: "Attendance",
                        principalColumn: "AttendanceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRegularizations_Users_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceRegularizations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRegularizations_ApprovedById",
                table: "AttendanceRegularizations",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRegularizations_AttendanceId",
                table: "AttendanceRegularizations",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRegularizations_UserId",
                table: "AttendanceRegularizations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRegularizations");
        }
    }
}
