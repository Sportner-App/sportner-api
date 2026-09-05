using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeriesId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesIntervalWeeks",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesSequence",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeriesTotalOccurrences",
                table: "Events",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_SeriesId_SeriesSequence",
                table: "Events",
                columns: new[] { "SeriesId", "SeriesSequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_SeriesId_SeriesSequence",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SeriesIntervalWeeks",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SeriesSequence",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SeriesTotalOccurrences",
                table: "Events");
        }
    }
}
