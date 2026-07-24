using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmployeeMonitoringAPI.Migrations
{
    /// <inheritdoc />
    public partial class CompleteSecureAdminModule20260714 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedPasswordChangeAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimaryAdmin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MfaEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangeLockedUntil",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "Projects",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Projects",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "RelatedUrl",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Departments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Departments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Departments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CompanyHolidays",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CompanyHolidays",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "CompanyHolidays",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CompanyHolidays",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingGroupId",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "CalendarEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CalendarEvents",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CalendarEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(@"
UPDATE Departments SET IsActive = 1, DepartmentCode = CONCAT('DEPT-', Id), CreatedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME();
UPDATE Projects SET ProjectCode = CONCAT('PRJ-', ProjectId), Priority = 'Medium', UpdatedAt = SYSUTCDATETIME();
UPDATE CompanyHolidays SET IsActive = 1, CreatedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME();
UPDATE CalendarEvents SET Status = 'Scheduled', MeetingGroupId = REPLACE(CONVERT(varchar(36), NEWID()), '-', ''), UpdatedAt = SYSUTCDATETIME();
UPDATE Notifications SET Type = 'General';
UPDATE Users SET IsPrimaryAdmin = 1 WHERE UserId = (SELECT TOP 1 UserId FROM Users WHERE Role = 'Admin' AND IsActive = 1 ORDER BY CreatedAt, UserId);
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CompanyName') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('CompanyName', 'Employee Monitoring System', 'Company', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CompanyTimezone') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('CompanyTimezone', 'Asia/Kolkata', 'Company', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'WorkingDays') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('WorkingDays', 'Monday,Tuesday,Wednesday,Thursday,Friday', 'Working Hours', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'StandardWorkHours') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('StandardWorkHours', '8', 'Working Hours', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'AttendanceLateAfter') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('AttendanceLateAfter', '09:15', 'Attendance Rules', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'SessionTimeoutMinutes') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('SessionTimeoutMinutes', '30', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'AccountLockoutThreshold') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('AccountLockoutThreshold', '5', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'MinimumPasswordLength') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('MinimumPasswordLength', '8', 'Password Policy', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'MfaEnabled') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('MfaEnabled', 'true', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'MfaRequiredForAdmin') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('MfaRequiredForAdmin', 'true', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'OtpLength') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('OtpLength', '6', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'OtpExpiryMinutes') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('OtpExpiryMinutes', '5', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'MaximumOtpAttempts') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('MaximumOtpAttempts', '5', 'Security', SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'OtpResendCooldownSeconds') INSERT INTO SystemSettings (SettingKey, SettingValue, Category, UpdatedAt) VALUES ('OtpResendCooldownSeconds', '60', 'Security', SYSUTCDATETIME());
");
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SafeMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MfaChallenges",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ChallengeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailedAttempts = table.Column<int>(type: "int", nullable: false),
                    ResendCount = table.Column<int>(type: "int", nullable: false),
                    LastSentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvalidatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaChallenges_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsPrimaryAdmin",
                table: "Users",
                column: "IsPrimaryAdmin",
                unique: true,
                filter: "[IsPrimaryAdmin] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DepartmentId",
                table: "Projects",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectCode",
                table: "Projects",
                column: "ProjectCode",
                unique: true,
                filter: "[ProjectCode] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_ProjectId",
                table: "CalendarEvents",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ChallengeId",
                table: "MfaChallenges",
                column: "ChallengeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_UserId",
                table: "MfaChallenges",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_Projects_ProjectId",
                table: "CalendarEvents",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "ProjectId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_Projects_ProjectId",
                table: "CalendarEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Departments_DepartmentId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "MfaChallenges");

            migrationBuilder.DropIndex(
                name: "IX_Users_IsPrimaryAdmin",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_DepartmentId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectCode",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_ProjectId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "FailedPasswordChangeAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsPrimaryAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastPasswordChangedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MfaEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangeLockedUntil",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RelatedUrl",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CompanyHolidays");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CompanyHolidays");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "CompanyHolidays");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyHolidays");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "MeetingGroupId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CalendarEvents");
        }
    }
}
