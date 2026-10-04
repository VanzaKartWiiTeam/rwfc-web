using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RetroRewindWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Badges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Badges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerBadges",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BadgeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerBadges", x => new { x.PlayerId, x.BadgeId });
                    table.ForeignKey(
                        name: "FK_PlayerBadges_Badges_BadgeId",
                        column: x => x.BadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerBadges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Badges",
                columns: new[] { "Id", "Name", "Role" },
                values: new object[,]
                {
                    { 61573, "Moderator", "ROLE_MODERATOR" },
                    { 61574, "Creator (VanzaKart)", "ROLE_LEADER" },
                    { 61575, "Staff Ghost", "ROLE_STAFF_GHOST" },
                    { 61576, "Developer", "ROLE_DEVELOPER" },
                    { 61577, "Creative Director", "ROLE_CREATIVE_DIRECTOR" },
                    { 61578, "Translator", "ROLE_TRANSLATOR" },
                    { 61579, "Beta Tester", "ROLE_BETA_TESTER" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Badges_Role",
                table: "Badges",
                column: "Role",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerBadges_BadgeId",
                table: "PlayerBadges",
                column: "BadgeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerBadges");

            migrationBuilder.DropTable(
                name: "Badges");
        }
    }
}
