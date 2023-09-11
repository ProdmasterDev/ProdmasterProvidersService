using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class adddisanid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DisanId",
                table: "Specifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DisanId",
                table: "Products",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisanId",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "DisanId",
                table: "Products");
        }
    }
}
