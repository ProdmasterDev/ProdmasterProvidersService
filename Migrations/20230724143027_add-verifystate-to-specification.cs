using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class addverifystatetospecification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Verified",
                table: "Specifications");

            migrationBuilder.AddColumn<int>(
                name: "VerifyState",
                table: "Specifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyState",
                table: "Specifications");

            migrationBuilder.AddColumn<bool>(
                name: "Verified",
                table: "Specifications",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
