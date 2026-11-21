using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Battles.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBattles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "battles");

            migrationBuilder.CreateTable(
                name: "Battles",
                schema: "battles",
                columns: table => new
                {
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArenaId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Round = table.Column<int>(type: "int", nullable: false),
                    WinnerId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.BattleId);
                });

            migrationBuilder.CreateTable(
                name: "BattleCombatants",
                schema: "battles",
                columns: table => new
                {
                    Slot = table.Column<int>(type: "int", nullable: false),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsBot = table.Column<bool>(type: "bit", nullable: false),
                    SpeciesId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HitPoints = table.Column<int>(type: "int", nullable: false),
                    MaxHitPoints = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleCombatants", x => new { x.BattleId, x.Slot });
                    table.ForeignKey(
                        name: "FK_BattleCombatants_Battles_BattleId",
                        column: x => x.BattleId,
                        principalSchema: "battles",
                        principalTable: "Battles",
                        principalColumn: "BattleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BattleCombatants_ParticipantId",
                schema: "battles",
                table: "BattleCombatants",
                column: "ParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BattleCombatants",
                schema: "battles");

            migrationBuilder.DropTable(
                name: "Battles",
                schema: "battles");
        }
    }
}
