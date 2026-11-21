using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Players.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExactIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot alter a column its primary key depends
            // on, so the key is dropped around the change.
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerStats",
                schema: "players",
                table: "PlayerStats");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerId",
                schema: "players",
                table: "PlayerStats",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerStats",
                schema: "players",
                table: "PlayerStats",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot alter a column its primary key depends
            // on, so the key is dropped around the change.
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerStats",
                schema: "players",
                table: "PlayerStats");

            migrationBuilder.AlterColumn<string>(
                name: "PlayerId",
                schema: "players",
                table: "PlayerStats",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerStats",
                schema: "players",
                table: "PlayerStats",
                column: "PlayerId");
        }
    }
}
