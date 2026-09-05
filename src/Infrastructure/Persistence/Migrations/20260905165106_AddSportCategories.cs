using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSportCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "Sports",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SportCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SportCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sports_CategoryId",
                table: "Sports",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_SportCategories_DisplayOrder",
                table: "SportCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SportCategories_IsActive",
                table: "SportCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SportCategories_Name",
                table: "SportCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SportCategories_Slug",
                table: "SportCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Sports_SportCategories_CategoryId",
                table: "Sports",
                column: "CategoryId",
                principalTable: "SportCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sports_SportCategories_CategoryId",
                table: "Sports");

            migrationBuilder.DropTable(
                name: "SportCategories");

            migrationBuilder.DropIndex(
                name: "IX_Sports_CategoryId",
                table: "Sports");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Sports");
        }
    }
}
