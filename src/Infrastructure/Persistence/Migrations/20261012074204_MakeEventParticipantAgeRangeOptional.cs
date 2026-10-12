using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeEventParticipantAgeRangeOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Age bounds become optional: null = no limit on that side.
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ParticipantAgeRange",
                table: "Events");

            migrationBuilder.AlterColumn<int>(
                name: "MinParticipantAge",
                table: "Events",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "MaxParticipantAge",
                table: "Events",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // Upcoming events got 18-60 only because the old form had no "no limit"
            // option; open them to all ages so applicants aren't asked for a birth date.
            migrationBuilder.Sql("UPDATE \"Events\" SET \"MinParticipantAge\" = NULL, \"MaxParticipantAge\" = NULL WHERE \"EventDate\" > now();");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ParticipantAgeRange",
                table: "Events",
                sql: "(\"MinParticipantAge\" IS NULL OR \"MinParticipantAge\" BETWEEN 13 AND 120) AND (\"MaxParticipantAge\" IS NULL OR \"MaxParticipantAge\" BETWEEN 13 AND 120) AND (\"MinParticipantAge\" IS NULL OR \"MaxParticipantAge\" IS NULL OR \"MinParticipantAge\" <= \"MaxParticipantAge\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_ParticipantAgeRange",
                table: "Events");

            // Unbounded sides fall back to the widest allowed range.
            migrationBuilder.Sql("UPDATE \"Events\" SET \"MinParticipantAge\" = 13 WHERE \"MinParticipantAge\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"Events\" SET \"MaxParticipantAge\" = 120 WHERE \"MaxParticipantAge\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "MinParticipantAge",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 13,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "MaxParticipantAge",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 120,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_ParticipantAgeRange",
                table: "Events",
                sql: "\"MinParticipantAge\" >= 13 AND \"MaxParticipantAge\" <= 120 AND \"MinParticipantAge\" <= \"MaxParticipantAge\"");
        }
    }
}
