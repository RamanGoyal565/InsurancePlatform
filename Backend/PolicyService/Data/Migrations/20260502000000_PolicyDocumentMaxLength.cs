using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class PolicyDocumentMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the 8000-char limit so base64-encoded PDFs (which can be 100KB+) can be stored
            migrationBuilder.AlterColumn<string>(
                name: "PolicyDocument",
                table: "Policies",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8000)",
                oldMaxLength: 8000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PolicyDocument",
                table: "Policies",
                type: "nvarchar(8000)",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
