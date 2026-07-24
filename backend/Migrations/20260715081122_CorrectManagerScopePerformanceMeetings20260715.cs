using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class CorrectManagerScopePerformanceMeetings20260715 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PerformanceReviews_ReviewerId_EmployeeId_Year_Quarter",
                table: "PerformanceReviews",
                columns: new[] { "ReviewerId", "EmployeeId", "Year", "Quarter" },
                unique: true,
                filter: "[ReviewerId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PerformanceReviews_ReviewerId_EmployeeId_Year_Quarter",
                table: "PerformanceReviews");
        }
    }
}
