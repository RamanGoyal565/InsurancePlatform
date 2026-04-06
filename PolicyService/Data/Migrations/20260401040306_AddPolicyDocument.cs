using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolicyService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PolicyDocument",
                table: "Policies",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PolicyDocument",
                table: "Policies");
        }
    }
}
