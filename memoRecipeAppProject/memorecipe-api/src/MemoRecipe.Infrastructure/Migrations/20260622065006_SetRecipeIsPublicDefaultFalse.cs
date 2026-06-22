using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoRecipe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetRecipeIsPublicDefaultFalse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Defense in depth: change the SQL default on the column
            // (covers direct INSERTs that omit the IsPublic column)
            migrationBuilder.AlterColumn<bool>(
                name: "IsPublic",
                table: "Recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            // Retroactive update: flip all existing recipes to private
            // (Privacy by Default - GDPR Art. 25). Safe here because the app
            // is not yet in public production (no real user data impacted).
            migrationBuilder.Sql("UPDATE \"Recipes\" SET \"IsPublic\" = FALSE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: restore the historical SQL default (true)
            migrationBuilder.AlterColumn<bool>(
                name: "IsPublic",
                table: "Recipes",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            // Note: previous IsPublic values per row are NOT restored
            // (we didn't snapshot them). Standard limitation of data-update rollbacks.
        }
    }
}
