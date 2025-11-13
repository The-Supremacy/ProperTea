using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheSupremacy.ProperSagas.Persistence.Ef.IntegrationTests.Migrations
{
    /// <inheritdoc />
    public partial class _2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Sagas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Sagas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Sagas");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Sagas");
        }
    }
}
