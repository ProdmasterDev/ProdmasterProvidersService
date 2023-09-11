using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class addusertospecification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Specifications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Specifications_UserId",
                table: "Specifications",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Specifications_Users_UserId",
                table: "Specifications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Specifications_Users_UserId",
                table: "Specifications");

            migrationBuilder.DropIndex(
                name: "IX_Specifications_UserId",
                table: "Specifications");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Specifications");
        }
    }
}
