using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventParticipantGender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "ParticipantGender",
                table: "Events",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ParticipantGender",
                table: "Events",
                sql: "\"ParticipantGender\" IS NULL OR \"ParticipantGender\" IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ParticipantGender",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ParticipantGender",
                table: "Events");
        }
    }
}
