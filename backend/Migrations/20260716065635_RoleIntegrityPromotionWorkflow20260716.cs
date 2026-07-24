using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class RoleIntegrityPromotionWorkflow20260716 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedDesignation",
                table: "PromotionHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DecisionDate",
                table: "PromotionHistories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagerRecommendation",
                table: "PromotionHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Quarter",
                table: "PromotionHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RecommendedById",
                table: "PromotionHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PromotionHistories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "PromotionHistories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedDesignation",
                table: "PerformanceReviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedDesignation",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "DecisionDate",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "ManagerRecommendation",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "Quarter",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "RecommendedById",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "PromotionHistories");

            migrationBuilder.DropColumn(
                name: "RecommendedDesignation",
                table: "PerformanceReviews");
        }
    }
}
