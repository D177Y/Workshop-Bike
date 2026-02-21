using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteLifecycleAndEmailRetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "CustomerProfiles",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNormalized",
                table: "CustomerProfiles",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceQuoteId",
                table: "Bookings",
                type: "longtext",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql(
                "UPDATE CustomerProfiles " +
                "SET PhoneNormalized = REGEXP_REPLACE(IFNULL(Phone, ''), '[^0-9]', '');");

            migrationBuilder.CreateTable(
                name: "EmailRetryQueueItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Recipient = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HtmlBody = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TextBody = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastAttemptUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    NextAttemptUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SentUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastError = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailRetryQueueItems", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_TenantId_Email",
                table: "CustomerProfiles",
                columns: new[] { "TenantId", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_TenantId_PhoneNormalized",
                table: "CustomerProfiles",
                columns: new[] { "TenantId", "PhoneNormalized" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailRetryQueueItems_Status_NextAttemptUtc",
                table: "EmailRetryQueueItems",
                columns: new[] { "Status", "NextAttemptUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailRetryQueueItems_TenantId_AccountNumber",
                table: "EmailRetryQueueItems",
                columns: new[] { "TenantId", "AccountNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailRetryQueueItems");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_TenantId_Email",
                table: "CustomerProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CustomerProfiles_TenantId_PhoneNormalized",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "PhoneNormalized",
                table: "CustomerProfiles");

            migrationBuilder.DropColumn(
                name: "SourceQuoteId",
                table: "Bookings");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "CustomerProfiles",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
