using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddYakAndLynx : Migration
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
                    { "avalanche", 3, "None", 0, 0, "Frost", "Avalanche", 160 },
                    { "pounce", 4, "None", 0, 0, "Nature", "Pounce", 200 },
                    { "thick-hide", 5, "Guard", 4, 40, "Neutral", "Thick Hide", 0 },
                    { "vine-whip", 1, "None", 0, 0, "Nature", "Vine Whip", 130 }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Species",
                columns: new[] { "Id", "Attack", "Defense", "Element", "MaxHitPoints", "Name", "Speed" },
                values: new object[,]
                {
                    { "glacier-yak", 9, 9, "Frost", 70, "Glacier Yak", 4 },
                    { "thorn-lynx", 13, 5, "Nature", 44, "Thorn Lynx", 11 }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "SpeciesAbilities",
                columns: new[] { "AbilityId", "SpeciesId", "Position" },
                values: new object[,]
                {
                    { "avalanche", "glacier-yak", 1 },
                    { "strike", "glacier-yak", 0 },
                    { "thick-hide", "glacier-yak", 2 },
                    { "pounce", "thorn-lynx", 2 },
                    { "strike", "thorn-lynx", 0 },
                    { "vine-whip", "thorn-lynx", 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "avalanche", "glacier-yak" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "strike", "glacier-yak" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "thick-hide", "glacier-yak" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "pounce", "thorn-lynx" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "strike", "thorn-lynx" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "SpeciesAbilities",
                keyColumns: new[] { "AbilityId", "SpeciesId" },
                keyValues: new object[] { "vine-whip", "thorn-lynx" });

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "avalanche");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "pounce");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "thick-hide");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "vine-whip");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "glacier-yak");

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "thorn-lynx");
        }
    }
}
