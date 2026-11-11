using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecurityHardening2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousRefreshTokenHash",
                table: "UserSessions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefreshTokenRotatedAt",
                table: "UserSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailVerificationFailedAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PasswordResetFailedAttempts",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Events.xmin Postgres sistem sütunu; eklenmez, model onu yalnızca concurrency token olarak okur.

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_PreviousRefreshTokenHash",
                table: "UserSessions",
                column: "PreviousRefreshTokenHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSessions_PreviousRefreshTokenHash",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "PreviousRefreshTokenHash",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "RefreshTokenRotatedAt",
                table: "UserSessions");

            migrationBuilder.DropColumn(
                name: "EmailVerificationFailedAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordResetFailedAttempts",
                table: "Users");

        }
    }
}
