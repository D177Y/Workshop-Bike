using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalDefaultsAndCategoryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AutoPriceRoundingIncrement",
                table: "CatalogSettings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0.50m);

            migrationBuilder.AddColumn<int>(
                name: "AutoPriceRoundingMode",
                table: "CatalogSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ServiceCategoryHierarchy",
                table: "CatalogSettings",
                type: "json",
                nullable: false,
                defaultValue: "{}")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GlobalServiceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Category1 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category2 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorHex = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalServiceCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GlobalServiceTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartNumber = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category1 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category2 = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SkillLevel = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultMinutes = table.Column<int>(type: "int", nullable: false),
                    BasePriceIncVat = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PricingMode = table.Column<int>(type: "int", nullable: false),
                    AutoPricingTier = table.Column<int>(type: "int", nullable: false),
                    EstimatedPriceIncVat = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalServiceTemplates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalServiceCategories_Category1_Category2",
                table: "GlobalServiceCategories",
                columns: new[] { "Category1", "Category2" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlobalServiceTemplates_Name_Category1_Category2",
                table: "GlobalServiceTemplates",
                columns: new[] { "Name", "Category1", "Category2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalServiceCategories");

            migrationBuilder.DropTable(
                name: "GlobalServiceTemplates");

            migrationBuilder.DropColumn(
                name: "AutoPriceRoundingIncrement",
                table: "CatalogSettings");

            migrationBuilder.DropColumn(
                name: "AutoPriceRoundingMode",
                table: "CatalogSettings");

            migrationBuilder.DropColumn(
                name: "ServiceCategoryHierarchy",
                table: "CatalogSettings");
        }
    }
}
