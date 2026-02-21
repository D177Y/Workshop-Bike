using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialYearSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinancialYearEndDay",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 31);

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearEndMonth",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearStartDay",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "FinancialYearStartMonth",
                table: "Tenants",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FinancialYearEndDay",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FinancialYearEndMonth",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FinancialYearStartDay",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FinancialYearStartMonth",
                table: "Tenants");
        }
    }
}
