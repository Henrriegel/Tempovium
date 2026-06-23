using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tempovium.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaSourceLastWriteTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalSourceLastWriteTimeUtc",
                table: "MediaItems",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalSourceLastWriteTimeUtc",
                table: "MediaItems");
        }
    }
}
