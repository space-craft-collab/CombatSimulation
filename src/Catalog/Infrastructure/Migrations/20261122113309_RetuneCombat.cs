using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RetuneCombat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "avalanche",
                columns: new[] { "Cooldown", "Power" },
                values: new object[] { 2, 180 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "chill-wind",
                columns: new[] { "EffectRounds", "EffectValue", "Power" },
                values: new object[] { 2, 40, 70 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "ember-bite",
                column: "EffectValue",
                value: 5);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "flame-dash",
                column: "Power",
                value: 160);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "ice-shard",
                column: "Power",
                value: 160);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "magma-claw",
                column: "Power",
                value: 160);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "pounce",
                columns: new[] { "Cooldown", "Power" },
                values: new object[] { 3, 220 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "regrow",
                columns: new[] { "Cooldown", "EffectValue" },
                values: new object[] { 3, 20 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "root-slam",
                column: "Cooldown",
                value: 2);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "shell-up",
                columns: new[] { "Cooldown", "EffectValue" },
                values: new object[] { 3, 60 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "thick-hide",
                columns: new[] { "Cooldown", "EffectValue" },
                values: new object[] { 3, 30 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "vine-whip",
                column: "Power",
                value: 140);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "ember-fox",
                columns: new[] { "Attack", "MaxHitPoints", "Speed" },
                values: new object[] { 21, 44, 14 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "frost-owl",
                columns: new[] { "Attack", "Defense", "MaxHitPoints" },
                values: new object[] { 21, 7, 39 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "glacier-yak",
                columns: new[] { "Attack", "Defense", "MaxHitPoints" },
                values: new object[] { 13, 8, 62 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "lava-crab",
                columns: new[] { "Attack", "Defense", "MaxHitPoints" },
                values: new object[] { 16, 7, 56 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "moss-golem",
                columns: new[] { "Attack", "Defense", "MaxHitPoints", "Speed" },
                values: new object[] { 14, 8, 68, 2 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "thorn-lynx",
                columns: new[] { "Attack", "Defense", "MaxHitPoints", "Speed" },
                values: new object[] { 21, 3, 43, 12 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "avalanche",
                columns: new[] { "Cooldown", "Power" },
                values: new object[] { 3, 160 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "chill-wind",
                columns: new[] { "EffectRounds", "EffectValue", "Power" },
                values: new object[] { 3, 50, 80 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "ember-bite",
                column: "EffectValue",
                value: 4);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "flame-dash",
                column: "Power",
                value: 150);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "ice-shard",
                column: "Power",
                value: 150);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "magma-claw",
                column: "Power",
                value: 140);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "pounce",
                columns: new[] { "Cooldown", "Power" },
                values: new object[] { 4, 200 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "regrow",
                columns: new[] { "Cooldown", "EffectValue" },
                values: new object[] { 4, 30 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "root-slam",
                column: "Cooldown",
                value: 3);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "shell-up",
                columns: new[] { "Cooldown", "EffectValue" },
                values: new object[] { 4, 50 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "thick-hide",
                columns: new[] { "Cooldown", "EffectValue" },
                values: new object[] { 5, 40 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Abilities",
                keyColumn: "Id",
                keyValue: "vine-whip",
                column: "Power",
                value: 130);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "ember-fox",
                columns: new[] { "Attack", "MaxHitPoints", "Speed" },
                values: new object[] { 14, 40, 12 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "frost-owl",
                columns: new[] { "Attack", "Defense", "MaxHitPoints" },
                values: new object[] { 12, 5, 35 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "glacier-yak",
                columns: new[] { "Attack", "Defense", "MaxHitPoints" },
                values: new object[] { 9, 9, 70 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "lava-crab",
                columns: new[] { "Attack", "Defense", "MaxHitPoints" },
                values: new object[] { 10, 8, 60 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "moss-golem",
                columns: new[] { "Attack", "Defense", "MaxHitPoints", "Speed" },
                values: new object[] { 8, 10, 80, 3 });

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Species",
                keyColumn: "Id",
                keyValue: "thorn-lynx",
                columns: new[] { "Attack", "Defense", "MaxHitPoints", "Speed" },
                values: new object[] { 13, 5, 44, 11 });
        }
    }
}
