using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceCategory2AndPricingModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AutoPricingTier",
                table: "JobDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category2",
                table: "JobDefinitions",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPriceIncVat",
                table: "JobDefinitions",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PricingMode",
                table: "JobDefinitions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<bool>(
                name: "AutomaticServicePricingEnabled",
                table: "CatalogSettings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultHourlyRate",
                table: "CatalogSettings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 76m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountedHourlyRate",
                table: "CatalogSettings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 60m);

            migrationBuilder.AddColumn<decimal>(
                name: "LossLeaderHourlyRate",
                table: "CatalogSettings",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 50m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoPricingTier",
                table: "JobDefinitions");

            migrationBuilder.DropColumn(
                name: "Category2",
                table: "JobDefinitions");

            migrationBuilder.DropColumn(
                name: "EstimatedPriceIncVat",
                table: "JobDefinitions");

            migrationBuilder.DropColumn(
                name: "PricingMode",
                table: "JobDefinitions");

            migrationBuilder.DropColumn(
                name: "AutomaticServicePricingEnabled",
                table: "CatalogSettings");

            migrationBuilder.DropColumn(
                name: "DefaultHourlyRate",
                table: "CatalogSettings");

            migrationBuilder.DropColumn(
                name: "DiscountedHourlyRate",
                table: "CatalogSettings");

            migrationBuilder.DropColumn(
                name: "LossLeaderHourlyRate",
                table: "CatalogSettings");
        }
    }
}
