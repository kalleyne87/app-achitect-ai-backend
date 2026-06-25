using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CloudMigrate.Business.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Requirements = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutiveSummary = table.Column<string>(type: "TEXT", nullable: false),
                    RecommendedServicesJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    RisksJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    TradeoffsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    RoadmapJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalRequest = table.Column<string>(type: "TEXT", nullable: false),
                    CollectedQuestionsAndAnswersJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    ConsolidatedPrompt = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentQuestionsJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "[]"),
                    Status = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "NeedsMoreInformation"),
                    FinalAssessmentJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "AssessmentSessions");
        }
    }
}
