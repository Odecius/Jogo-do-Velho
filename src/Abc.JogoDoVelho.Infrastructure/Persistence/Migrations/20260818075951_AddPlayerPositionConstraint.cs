using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abc.JogoDoVelho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerPositionConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_players_position",
                table: "players",
                sql: "\"Position\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_players_position",
                table: "players");
        }
    }
}
