using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FeeAmount",
                table: "Events",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPaid",
                table: "Events",
                column: "IsPaid");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Fee",
                table: "Events",
                sql: "(\"IsPaid\" = FALSE AND \"FeeAmount\" IS NULL) OR (\"IsPaid\" = TRUE AND \"FeeAmount\" IS NOT NULL AND \"FeeAmount\" > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_IsPaid",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Fee",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "FeeAmount",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Events");
        }
    }
}
