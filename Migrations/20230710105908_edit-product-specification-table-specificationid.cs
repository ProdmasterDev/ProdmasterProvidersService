using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProdmasterProvidersService.Migrations
{
    /// <inheritdoc />
    public partial class editproductspecificationtablespecificationid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSpecifications_Specifications_SpecficationId",
                table: "ProductSpecifications");

            migrationBuilder.RenameColumn(
                name: "SpecficationId",
                table: "ProductSpecifications",
                newName: "SpecificationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSpecifications_SpecficationId",
                table: "ProductSpecifications",
                newName: "IX_ProductSpecifications_SpecificationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSpecifications_Specifications_SpecificationId",
                table: "ProductSpecifications",
                column: "SpecificationId",
                principalTable: "Specifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductSpecifications_Specifications_SpecificationId",
                table: "ProductSpecifications");

            migrationBuilder.RenameColumn(
                name: "SpecificationId",
                table: "ProductSpecifications",
                newName: "SpecficationId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductSpecifications_SpecificationId",
                table: "ProductSpecifications",
                newName: "IX_ProductSpecifications_SpecficationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductSpecifications_Specifications_SpecficationId",
                table: "ProductSpecifications",
                column: "SpecficationId",
                principalTable: "Specifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
