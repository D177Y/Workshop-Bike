using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddServicePackageAddOnReductions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServicePackageAddOnTimeReductions",
                table: "CatalogSettings",
                type: "json",
                nullable: false,
                defaultValue: "{}")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServicePackageAddOnTimeReductions",
                table: "CatalogSettings");
        }
    }
}
