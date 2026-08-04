using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MineTimbermanBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlaySessionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "SelectedSide",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Characters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "Characters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SelectedSide",
                table: "Characters",
                type: "TEXT",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Characters",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }
    }
}
