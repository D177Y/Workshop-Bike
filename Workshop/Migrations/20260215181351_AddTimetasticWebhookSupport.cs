using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddTimetasticWebhookSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimetasticPassword",
                table: "IntegrationSettings");

            migrationBuilder.RenameColumn(
                name: "TimetasticUsername",
                table: "IntegrationSettings",
                newName: "TimetasticWebhookSecret");

            migrationBuilder.AddColumn<DateTime>(
                name: "TimetasticLastWebhookReceivedUtc",
                table: "IntegrationSettings",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TimetasticWebhookEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ProcessedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Outcome = table.Column<string>(type: "varchar(240)", maxLength: 240, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetasticWebhookEvents", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TimetasticWebhookEvents_TenantId_EventId",
                table: "TimetasticWebhookEvents",
                columns: new[] { "TenantId", "EventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetasticWebhookEvents_TenantId_ReceivedUtc",
                table: "TimetasticWebhookEvents",
                columns: new[] { "TenantId", "ReceivedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimetasticWebhookEvents");

            migrationBuilder.DropColumn(
                name: "TimetasticLastWebhookReceivedUtc",
                table: "IntegrationSettings");

            migrationBuilder.RenameColumn(
                name: "TimetasticWebhookSecret",
                table: "IntegrationSettings",
                newName: "TimetasticUsername");

            migrationBuilder.AddColumn<string>(
                name: "TimetasticPassword",
                table: "IntegrationSettings",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
