using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddJobPackageOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PackageOverrides",
                table: "JobDefinitions",
                type: "json",
                nullable: false,
                defaultValue: "[]")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PackageOverrides",
                table: "JobDefinitions");
        }
    }
}
