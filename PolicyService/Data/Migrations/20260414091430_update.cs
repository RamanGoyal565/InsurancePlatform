using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaymentFailedOnUtc",
                table: "CustomerPolicies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastPaymentFailureReason",
                table: "CustomerPolicies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPaymentFailedOnUtc",
                table: "CustomerPolicies");

            migrationBuilder.DropColumn(
                name: "LastPaymentFailureReason",
                table: "CustomerPolicies");
        }
    }
}
