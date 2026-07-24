using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class CompleteManagerModule20260715 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExpectedOutput",
                table: "WorkTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerNotes",
                table: "WorkTasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "WorkTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Collaboration",
                table: "PerformanceReviews",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Communication",
                table: "PerformanceReviews",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ImprovementAreas",
                table: "PerformanceReviews",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsReleased",
                table: "PerformanceReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ProjectContribution",
                table: "PerformanceReviews",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Quarter",
                table: "PerformanceReviews",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Strengths",
                table: "PerformanceReviews",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TechnicalContribution",
                table: "PerformanceReviews",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Timeliness",
                table: "PerformanceReviews",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WorkQuality",
                table: "PerformanceReviews",
                type: "decimal(3,1)",
                precision: 3,
                scale: 1,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "PerformanceReviews",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedOutput",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "ManagerNotes",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "WorkTasks");

            migrationBuilder.DropColumn(
                name: "Collaboration",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Communication",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "ImprovementAreas",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "IsReleased",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "ProjectContribution",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Quarter",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Strengths",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "TechnicalContribution",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Timeliness",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "WorkQuality",
                table: "PerformanceReviews");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "PerformanceReviews");
        }
    }
}
