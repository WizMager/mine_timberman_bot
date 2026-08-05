using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MineTimbermanBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLuckyToCharacters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Lucky",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lucky",
                table: "Characters");
        }
    }
}
