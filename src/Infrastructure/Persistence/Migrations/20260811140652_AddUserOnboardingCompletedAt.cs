using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboardingCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingCompletedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                table: "Users");
        }
    }
}
