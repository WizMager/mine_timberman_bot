using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MineTimbermanBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    BoltsInWorkSession = table.Column<int>(type: "integer", nullable: false),
                    LogsInWorkSession = table.Column<int>(type: "integer", nullable: false),
                    Force = table.Column<int>(type: "integer", nullable: false),
                    LastWorkTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LastRestTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ChatMemberships",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMemberships", x => new { x.ChatId, x.UserId });
                });

            migrationBuilder.CreateTable(
                name: "Duels",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    StatusMessageId = table.Column<int>(type: "integer", nullable: false),
                    ChallengerUserId = table.Column<long>(type: "bigint", nullable: false),
                    OpponentUserId = table.Column<long>(type: "bigint", nullable: false),
                    ChallengerName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OpponentName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChallengerDmMessageId = table.Column<int>(type: "integer", nullable: true),
                    OpponentDmMessageId = table.Column<int>(type: "integer", nullable: true),
                    ChallengerChoice = table.Column<int>(type: "integer", nullable: true),
                    OpponentChoice = table.Column<int>(type: "integer", nullable: true),
                    ChallengerChoiceAuto = table.Column<bool>(type: "boolean", nullable: false),
                    OpponentChoiceAuto = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Duels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMemberships_ChatId",
                table: "ChatMemberships",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMemberships_UserId",
                table: "ChatMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Duels_ChallengerUserId",
                table: "Duels",
                column: "ChallengerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Duels_OpponentUserId",
                table: "Duels",
                column: "OpponentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "ChatMemberships");

            migrationBuilder.DropTable(
                name: "Duels");
        }
    }
}
