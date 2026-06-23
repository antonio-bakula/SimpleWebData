using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleWebData.Migrations
{
    /// <inheritdoc />
    public partial class AddWebSiteName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "WebSites",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "WebSites");
        }
    }
}
