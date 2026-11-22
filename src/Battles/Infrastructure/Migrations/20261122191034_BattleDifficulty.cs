using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Battles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BattleDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Difficulty",
                schema: "battles",
                table: "Battles",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                // Battles stored before difficulties existed were
                // played on Normal; an empty name would not read back.
                defaultValue: "Normal");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Difficulty",
                schema: "battles",
                table: "Battles");
        }
    }
}
