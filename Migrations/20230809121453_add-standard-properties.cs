using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class addstandardproperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Standarts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Consistenc",
                table: "Standarts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Smell",
                table: "Standarts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Structure",
                table: "Standarts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Taste",
                table: "Standarts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Standarts");

            migrationBuilder.DropColumn(
                name: "Consistenc",
                table: "Standarts");

            migrationBuilder.DropColumn(
                name: "Smell",
                table: "Standarts");

            migrationBuilder.DropColumn(
                name: "Structure",
                table: "Standarts");

            migrationBuilder.DropColumn(
                name: "Taste",
                table: "Standarts");
        }
    }
}
