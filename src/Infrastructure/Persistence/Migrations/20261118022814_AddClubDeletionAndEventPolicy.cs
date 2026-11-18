using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubDeletionAndEventPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Organizations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "EventCreationPolicy",
                table: "Organizations",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Organizations.xmin ve OrganizationMembers.xmin Postgres sistem sütunları; eklenmez,
            // model onları yalnızca concurrency token olarak okur (bkz. SecurityHardening2).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "EventCreationPolicy",
                table: "Organizations");
        }
    }
}
