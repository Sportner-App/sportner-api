using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClubVisibilityAndImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Organizations",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "Organizations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOrganizationOnly",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Mevcut tüm kulüp etkinlikleri bugüne kadar yalnızca üyelere açıktı; davranış değişmesin.
            migrationBuilder.Sql(
                "UPDATE \"Events\" SET \"IsOrganizationOnly\" = TRUE WHERE \"OrganizationId\" IS NOT NULL;");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_IsPublic",
                table: "Organizations",
                column: "IsPublic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_IsPublic",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "IsOrganizationOnly",
                table: "Events");
        }
    }
}
