using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSupremacy.ProperSagas.Persistence.Ef.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Sagas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SagaType = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    LockToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SagaData = table.Column<string>(type: "text", nullable: false),
                    Steps = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    TraceId = table.Column<string>(type: "text", nullable: true),
                    IsCancellationRequested = table.Column<bool>(type: "boolean", nullable: false),
                    CancellationRequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeoutDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sagas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_CorrelationId",
                table: "Sagas",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_CreatedAt",
                table: "Sagas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_SagaType",
                table: "Sagas",
                column: "SagaType");

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_Status",
                table: "Sagas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_Status_LockedAt",
                table: "Sagas",
                columns: new[] { "Status", "LockedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Sagas_Status_TimeoutDeadline",
                table: "Sagas",
                columns: new[] { "Status", "TimeoutDeadline" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Sagas");
        }
    }
}
