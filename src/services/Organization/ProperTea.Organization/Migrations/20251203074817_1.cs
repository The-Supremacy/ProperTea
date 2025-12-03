using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProperTea.Organization.Migrations
{
    /// <inheritdoc />
    public partial class A1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Alias",
                table: "Organizations",
                newName: "OrgAlias");

            migrationBuilder.RenameIndex(
                name: "IX_Organizations_Alias",
                table: "Organizations",
                newName: "IX_Organizations_OrgAlias");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrgAlias",
                table: "Organizations",
                newName: "Alias");

            migrationBuilder.RenameIndex(
                name: "IX_Organizations_OrgAlias",
                table: "Organizations",
                newName: "IX_Organizations_Alias");
        }
    }
}
