using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeSubscriptionLifecycleState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasActivatedSubscription",
                table: "Tenants",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeCurrentPeriodEndUtc",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionStatus",
                table: "Tenants",
                type: "varchar(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "StripeSubscriptionUpdatedAtUtc",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_HasActivatedSubscription",
                table: "Tenants",
                column: "HasActivatedSubscription");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_StripeSubscriptionStatus",
                table: "Tenants",
                column: "StripeSubscriptionStatus");

            migrationBuilder.Sql("""
                UPDATE `Tenants`
                SET `HasActivatedSubscription` = 1
                WHERE COALESCE(`StripeSubscriptionId`, '') <> '';
                """);

            migrationBuilder.Sql("""
                UPDATE `Tenants`
                SET `StripeSubscriptionStatus` = CASE
                    WHEN `IsActive` = 1 THEN 'active'
                    ELSE 'canceled'
                END
                WHERE COALESCE(`StripeSubscriptionId`, '') <> ''
                  AND COALESCE(`StripeSubscriptionStatus`, '') = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_HasActivatedSubscription",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_StripeSubscriptionStatus",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "HasActivatedSubscription",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeCurrentPeriodEndUtc",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionStatus",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionUpdatedAtUtc",
                table: "Tenants");
        }
    }
}
