using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeRecruitmentCandidates20260715 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applicants_JobId",
                table: "Applicants");

            migrationBuilder.AddColumn<int>(
                name: "CandidateId",
                table: "Applicants",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecruitmentCandidates",
                columns: table => new
                {
                    CandidateId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedHrId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentCandidates", x => x.CandidateId);
                });

            migrationBuilder.Sql(@"
                INSERT INTO RecruitmentCandidates (AssignedHrId, FullName, Email, NormalizedEmail, Phone, CreatedAt)
                SELECT AssignedHrId, MAX(FullName), MAX(Email), NormalizedEmail, MAX(Phone), GETUTCDATE()
                FROM Applicants
                GROUP BY AssignedHrId, NormalizedEmail;

                UPDATE a SET CandidateId = c.CandidateId
                FROM Applicants a
                INNER JOIN RecruitmentCandidates c
                    ON c.AssignedHrId = a.AssignedHrId AND c.NormalizedEmail = a.NormalizedEmail;");

            migrationBuilder.AlterColumn<int>(
                name: "CandidateId",
                table: "Applicants",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
            migrationBuilder.CreateIndex(
                name: "IX_Applicants_CandidateId",
                table: "Applicants",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_JobId_CandidateId",
                table: "Applicants",
                columns: new[] { "JobId", "CandidateId" },
                unique: true,
                filter: "[JobId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCandidates_AssignedHrId_NormalizedEmail",
                table: "RecruitmentCandidates",
                columns: new[] { "AssignedHrId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Applicants_RecruitmentCandidates_CandidateId",
                table: "Applicants",
                column: "CandidateId",
                principalTable: "RecruitmentCandidates",
                principalColumn: "CandidateId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Applicants_RecruitmentCandidates_CandidateId",
                table: "Applicants");

            migrationBuilder.DropTable(
                name: "RecruitmentCandidates");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_CandidateId",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_JobId_CandidateId",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "Applicants");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_JobId",
                table: "Applicants",
                column: "JobId");
        }
    }
}
