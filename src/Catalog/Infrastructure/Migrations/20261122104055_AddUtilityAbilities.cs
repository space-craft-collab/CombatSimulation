using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUtilityAbilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Abilities",
                columns: new[] { "Id", "Cooldown", "Effect", "EffectRounds", "EffectValue", "Element", "Name", "Power" },
                values: new object[,]
                {
                    { "chill-wind", 3, "Slow", 3, 50, "Frost", "Chill Wind", 80 },
                    { "ember-bite", 3, "Burn", 3, 4, "Fire", "Ember Bite", 80 },
                    { "regrow", 4, "Heal", 0, 30, "Nature", "Regrow", 0 },
                    { "shell-up", 4, "Guard", 3, 50, "Neutral", "Shell Up", 0 }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "SpeciesAbilities",
                columns: new[] { "AbilityId", "SpeciesId", "Position" },
                values: new object[,]
                {
                    { "ember-bite", "ember-fox", 2 },
                    { "chill-wind", "frost-owl", 2 },
                    { "shell-up", "lava-crab", 2 },
                    { "regrow", "moss-golem", 2 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "ember-bite", "ember-fox" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "chill-wind", "frost-owl" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "shell-up", "lava-crab" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "regrow", "moss-golem" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "chill-wind");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "ember-bite");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "regrow");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "shell-up");
        }
    }
}
