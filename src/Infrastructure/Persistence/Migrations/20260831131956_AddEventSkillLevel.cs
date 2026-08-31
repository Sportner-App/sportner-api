using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSkillLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "SkillLevel",
                table: "Events",
                type: "smallint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_SkillLevel",
                table: "Events",
                column: "SkillLevel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_SkillLevel",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SkillLevel",
                table: "Events");
        }
    }
}
