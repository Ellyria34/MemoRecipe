using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoRecipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteRequestedAtToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeleteRequestedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleteRequestedAt",
                table: "Users");
        }
    }
}
