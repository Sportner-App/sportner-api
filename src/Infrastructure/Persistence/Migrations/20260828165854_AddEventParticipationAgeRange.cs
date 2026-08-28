using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventParticipationAgeRange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxParticipantAge",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<int>(
                name: "MinParticipantAge",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 18);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ParticipantAgeRange",
                table: "Events",
                sql: "\"MinParticipantAge\" >= 13 AND \"MaxParticipantAge\" <= 120 AND \"MinParticipantAge\" <= \"MaxParticipantAge\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ParticipantAgeRange",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MaxParticipantAge",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MinParticipantAge",
                table: "Events");
        }
    }
}
