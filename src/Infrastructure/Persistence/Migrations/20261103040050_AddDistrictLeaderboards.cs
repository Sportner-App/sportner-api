using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictLeaderboards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowOnLeaderboards",
                table: "UserProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Events",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DistrictLeaders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Month = table.Column<DateOnly>(type: "date", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventCount = table.Column<int>(type: "integer", nullable: false),
                    CrownedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistrictLeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistrictLeaders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Events_City_District_EventDate",
                table: "Events",
                columns: new[] { "City", "District", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DistrictLeaders_City_District_Month",
                table: "DistrictLeaders",
                columns: new[] { "City", "District", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DistrictLeaders_UserId",
                table: "DistrictLeaders",
                column: "UserId");

            // Mevcut etkinliklerin semt/şehrini adresten doldur (EventAddressParts.Pattern ile aynı).
            migrationBuilder.Sql("""
                UPDATE "Events" AS e
                SET "District" = btrim(m[1]), "City" = btrim(m[2])
                FROM (
                    SELECT "Id", regexp_match("Address", '(?:\d{5}\s+)?([^,/\d\s][^,/]*)/([^,/]+),\s*(?:Türkiye|Turkey)\s*$') AS m
                    FROM "Events"
                ) AS parsed
                WHERE parsed."Id" = e."Id" AND parsed.m IS NOT NULL;
                """);

            migrationBuilder.Sql("ALTER TABLE \"DistrictLeaders\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DistrictLeaders");

            migrationBuilder.DropIndex(
                name: "IX_Events_City_District_EventDate",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ShowOnLeaderboards",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Events");
        }
    }
}
