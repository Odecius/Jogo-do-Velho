using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Abc.JogoDoVelho.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerAvatarMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarContentType",
                table: "players",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvatarExpiresAtUtc",
                table: "players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvatarStorageName",
                table: "players",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvatarUploadedAtUtc",
                table: "players",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_AvatarExpiresAtUtc",
                table: "players",
                column: "AvatarExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_players_AvatarExpiresAtUtc",
                table: "players");

            migrationBuilder.DropColumn(
                name: "AvatarContentType",
                table: "players");

            migrationBuilder.DropColumn(
                name: "AvatarExpiresAtUtc",
                table: "players");

            migrationBuilder.DropColumn(
                name: "AvatarStorageName",
                table: "players");

            migrationBuilder.DropColumn(
                name: "AvatarUploadedAtUtc",
                table: "players");
        }
    }
}
