using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class LeaveLifecycleHistoryAndPreciseBalance20260722 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Leaves",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationRequestedAt",
                table: "Leaves",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UsedLeaves",
                table: "LeaveBalances",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalLeaves",
                table: "LeaveBalances",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "LeaveHistories",
                columns: table => new
                {
                    LeaveHistoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveId = table.Column<int>(type: "int", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ActorUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveHistories", x => x.LeaveHistoryId);
                    table.ForeignKey(
                        name: "FK_LeaveHistories_Leaves_LeaveId",
                        column: x => x.LeaveId,
                        principalTable: "Leaves",
                        principalColumn: "LeaveId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveHistories_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveHistories_ActorUserId",
                table: "LeaveHistories",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveHistories_LeaveId",
                table: "LeaveHistories",
                column: "LeaveId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaveHistories");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Leaves");

            migrationBuilder.DropColumn(
                name: "CancellationRequestedAt",
                table: "Leaves");

            migrationBuilder.AlterColumn<int>(
                name: "UsedLeaves",
                table: "LeaveBalances",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2);

            migrationBuilder.AlterColumn<int>(
                name: "TotalLeaves",
                table: "LeaveBalances",
                type: "int",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(6,2)",
                oldPrecision: 6,
                oldScale: 2);
        }
    }
}
