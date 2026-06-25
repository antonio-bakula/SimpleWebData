using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleWebData.Migrations
{
    /// <inheritdoc />
    public partial class RemovePageFacilityPhotoGalleryLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_PhotoGalleries_PhotoGalleryId",
                table: "Facilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Pages_PhotoGalleries_PhotoGalleryId",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Pages_PhotoGalleryId",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_PhotoGalleryId",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PhotoGalleryId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "PhotoGalleryId",
                table: "Facilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PhotoGalleryId",
                table: "Pages",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhotoGalleryId",
                table: "Facilities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PhotoGalleryId",
                table: "Pages",
                column: "PhotoGalleryId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_PhotoGalleryId",
                table: "Facilities",
                column: "PhotoGalleryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_PhotoGalleries_PhotoGalleryId",
                table: "Facilities",
                column: "PhotoGalleryId",
                principalTable: "PhotoGalleries",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_PhotoGalleries_PhotoGalleryId",
                table: "Pages",
                column: "PhotoGalleryId",
                principalTable: "PhotoGalleries",
                principalColumn: "Id");
        }
    }
}
