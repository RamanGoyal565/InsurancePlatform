using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TicketService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentToComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentBase64",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentFileName",
                table: "Comments",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentBase64",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "DocumentFileName",
                table: "Comments");
        }
    }
}
