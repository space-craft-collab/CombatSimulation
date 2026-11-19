using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "Species",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MaxHitPoints = table.Column<int>(type: "int", nullable: false),
                    Attack = table.Column<int>(type: "int", nullable: false),
                    Defense = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Species", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Species",
                columns: new[] { "Id", "Attack", "Defense", "MaxHitPoints", "Name", "Speed" },
                values: new object[,]
                {
                    { "ember-fox", 14, 4, 40, "Ember Fox", 12 },
                    { "frost-owl", 12, 5, 35, "Frost Owl", 14 },
                    { "lava-crab", 10, 8, 60, "Lava Crab", 5 },
                    { "moss-golem", 8, 10, 80, "Moss Golem", 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Species",
                schema: "catalog");
        }
    }
}
