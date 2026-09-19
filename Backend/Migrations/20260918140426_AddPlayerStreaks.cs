using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetroRewindWebsite.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerStreaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerStreaks",
                columns: table => new
                {
                    ProfileId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentStreak = table.Column<int>(type: "integer", nullable: false),
                    BestStreak = table.Column<int>(type: "integer", nullable: false),
                    StreakStartDay = table.Column<DateOnly>(type: "date", nullable: true),
                    LastActiveDay = table.Column<DateOnly>(type: "date", nullable: true),
                    LostStreak = table.Column<int>(type: "integer", nullable: false),
                    LostStreakStartDay = table.Column<DateOnly>(type: "date", nullable: true),
                    LostStreakLastDay = table.Column<DateOnly>(type: "date", nullable: true),
                    VacationSince = table.Column<DateOnly>(type: "date", nullable: true),
                    RestoreMonth = table.Column<int>(type: "integer", nullable: false),
                    RestoresUsed = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStreaks", x => x.ProfileId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStreaks_CurrentStreak",
                table: "PlayerStreaks",
                column: "CurrentStreak");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStreaks_LastActiveDay",
                table: "PlayerStreaks",
                column: "LastActiveDay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerStreaks");
        }
    }
}
