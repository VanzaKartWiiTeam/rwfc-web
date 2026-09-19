using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroRewindWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddPrestigeRank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrestigeRank",
                table: "Players",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrestigeRank",
                table: "Players");
        }
    }
}
