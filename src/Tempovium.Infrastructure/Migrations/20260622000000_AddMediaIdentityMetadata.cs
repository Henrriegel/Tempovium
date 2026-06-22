using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tempovium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaIdentityMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "MediaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "OriginalSourcePath",
                table: "MediaItems",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "OriginalSourcePath",
                table: "MediaItems");
        }
    }
}
