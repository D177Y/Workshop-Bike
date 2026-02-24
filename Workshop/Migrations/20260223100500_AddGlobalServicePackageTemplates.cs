using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Workshop.Data;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WorkshopDbContext))]
    [Migration("20260223100500_AddGlobalServicePackageTemplates")]
    public partial class AddGlobalServicePackageTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalServicePackageTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SkillLevel = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultMinutes = table.Column<int>(type: "int", nullable: false),
                    BasePriceIncVat = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    PricingMode = table.Column<int>(type: "int", nullable: false),
                    AutoPricingTier = table.Column<int>(type: "int", nullable: false),
                    EstimatedPriceIncVat = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Items = table.Column<string>(type: "json", nullable: false, defaultValue: "[]")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalServicePackageTemplates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_GlobalServicePackageTemplates_Name",
                table: "GlobalServicePackageTemplates",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalServicePackageTemplates");
        }
    }
}
