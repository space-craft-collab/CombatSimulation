using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddElements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Element",
                schema: "catalog",
                table: "Species",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "ember-fox",
                column: "Element",
                value: "Fire");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "frost-owl",
                column: "Element",
                value: "Frost");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "lava-crab",
                column: "Element",
                value: "Fire");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "moss-golem",
                column: "Element",
                value: "Nature");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Element",
                schema: "catalog",
                table: "Species");
        }
    }
}
