using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSupremacy.ProperSagas.Persistence.Ef.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class _1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledFor",
                table: "Sagas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Sagas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledFor",
                table: "Sagas");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Sagas");
        }
    }
}
