using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class addverifyNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerifyNote",
                table: "Specifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifyNote",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerifyState",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyNote",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "VerifyNote",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VerifyState",
                table: "Products");
        }
    }
}
