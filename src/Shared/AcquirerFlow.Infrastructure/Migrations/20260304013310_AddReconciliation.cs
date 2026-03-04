using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AcquirerFlow.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReconciliationReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReferenceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalTransactions = table.Column<int>(type: "int", nullable: false),
                    TotalCaptures = table.Column<int>(type: "int", nullable: false),
                    TotalSettlements = table.Column<int>(type: "int", nullable: false),
                    DiscrepancyCount = table.Column<int>(type: "int", nullable: false),
                    TotalAuthorizedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCapturedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSettledGrossAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalSettledNetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CaptureId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SettlementBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MerchantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiscrepancyType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpectedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationEntries_ReconciliationReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "ReconciliationReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationEntries_MerchantId",
                table: "ReconciliationEntries",
                column: "MerchantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationEntries_ReportId",
                table: "ReconciliationEntries",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationReports_ReferenceDate",
                table: "ReconciliationReports",
                column: "ReferenceDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReconciliationEntries");

            migrationBuilder.DropTable(
                name: "ReconciliationReports");
        }
    }
}
