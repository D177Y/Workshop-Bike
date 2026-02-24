using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workshop.Migrations
{
    /// <inheritdoc />
    public partial class AddTrialLifecycleRetentionAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TrialDataPurgedAtUtc",
                table: "Tenants",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrialExitFeedbackEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Disliked = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Improvements = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoSignupReason = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrialExitFeedbackEntries", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_TrialDataPurgedAtUtc",
                table: "Tenants",
                column: "TrialDataPurgedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TrialExitFeedbackEntries_TenantId_SubmittedAtUtc",
                table: "TrialExitFeedbackEntries",
                columns: new[] { "TenantId", "SubmittedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrialExitFeedbackEntries");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_TrialDataPurgedAtUtc",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "TrialDataPurgedAtUtc",
                table: "Tenants");
        }
    }
}
