using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Migrations
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
                name: "PK_Arenas",
                schema: "catalog",
                table: "Arenas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Species",
                schema: "catalog",
                table: "Species");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: "catalog",
                table: "Species",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: "catalog",
                table: "Arenas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Arenas",
                schema: "catalog",
                table: "Arenas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Species",
                schema: "catalog",
                table: "Species",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot alter a column its primary key depends
            // on, so the key is dropped around the change.
            migrationBuilder.DropPrimaryKey(
                name: "PK_Arenas",
                schema: "catalog",
                table: "Arenas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Species",
                schema: "catalog",
                table: "Species");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: "catalog",
                table: "Species",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: "catalog",
                table: "Arenas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Arenas",
                schema: "catalog",
                table: "Arenas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Species",
                schema: "catalog",
                table: "Species",
                column: "Id");
        }
    }
}
