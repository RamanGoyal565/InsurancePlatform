using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePolicyWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Type",
                table: "Policies");

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Policies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DrivingLicenseNumber",
                table: "CustomerPolicies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiredNotifiedOnUtc",
                table: "CustomerPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFinalWeekReminderSentOnUtc",
                table: "CustomerPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMonthlyWindowReminderSentOnUtc",
                table: "CustomerPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingOperation",
                table: "CustomerPolicies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "CustomerPolicies",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "DrivingLicenseNumber",
                table: "CustomerPolicies");

            migrationBuilder.DropColumn(
                name: "ExpiredNotifiedOnUtc",
                table: "CustomerPolicies");

            migrationBuilder.DropColumn(
                name: "LastFinalWeekReminderSentOnUtc",
                table: "CustomerPolicies");

            migrationBuilder.DropColumn(
                name: "LastMonthlyWindowReminderSentOnUtc",
                table: "CustomerPolicies");

            migrationBuilder.DropColumn(
                name: "PendingOperation",
                table: "CustomerPolicies");

            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "CustomerPolicies");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Policies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
