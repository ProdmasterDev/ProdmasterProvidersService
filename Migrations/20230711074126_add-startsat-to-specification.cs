using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class addstartsattospecification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Expires",
                table: "Specifications",
                newName: "StartsAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Specifications",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Specifications");

            migrationBuilder.RenameColumn(
                name: "StartsAt",
                table: "Specifications",
                newName: "Expires");
        }
    }
}
