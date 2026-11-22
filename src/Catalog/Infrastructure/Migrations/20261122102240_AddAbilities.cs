using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAbilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Abilities",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Element = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Power = table.Column<int>(type: "int", nullable: false),
                    Cooldown = table.Column<int>(type: "int", nullable: false),
                    Effect = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    EffectValue = table.Column<int>(type: "int", nullable: false),
                    EffectRounds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpeciesAbilities",
                schema: "catalog",
                columns: table => new
                {
                    SpeciesId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    AbilityId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesAbilities", x => new { x.SpeciesId, x.AbilityId });
                    table.ForeignKey(
                        name: "FK_SpeciesAbilities_Abilities_AbilityId",
                        column: x => x.AbilityId,
                        principalSchema: "catalog",
                        principalTable: "Abilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpeciesAbilities_Species_SpeciesId",
                        column: x => x.SpeciesId,
                        principalSchema: "catalog",
                        principalTable: "Species",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Abilities",
                columns: new[] { "Id", "Cooldown", "Effect", "EffectRounds", "EffectValue", "Element", "Name", "Power" },
                values: new object[,]
                {
                    { "flame-dash", 2, "None", 0, 0, "Fire", "Flame Dash", 150 },
                    { "ice-shard", 2, "None", 0, 0, "Frost", "Ice Shard", 150 },
                    { "magma-claw", 2, "None", 0, 0, "Fire", "Magma Claw", 140 },
                    { "root-slam", 3, "None", 0, 0, "Nature", "Root Slam", 150 },
                    { "strike", 0, "None", 0, 0, "Neutral", "Strike", 100 }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "SpeciesAbilities",
                columns: new[] { "AbilityId", "SpeciesId", "Position" },
                values: new object[,]
                {
                    { "flame-dash", "ember-fox", 1 },
                    { "strike", "ember-fox", 0 },
                    { "ice-shard", "frost-owl", 1 },
                    { "strike", "frost-owl", 0 },
                    { "magma-claw", "lava-crab", 1 },
                    { "strike", "lava-crab", 0 },
                    { "root-slam", "moss-golem", 1 },
                    { "strike", "moss-golem", 0 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpeciesAbilities_AbilityId",
                schema: "catalog",
                table: "SpeciesAbilities",
                column: "AbilityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpeciesAbilities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "Abilities",
                schema: "catalog");
        }
    }
}
