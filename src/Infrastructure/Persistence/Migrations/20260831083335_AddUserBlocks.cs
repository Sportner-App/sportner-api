using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                    table.CheckConstraint("CK_UserBlocks_NotSelf", "\"BlockerUserId\" <> \"BlockedUserId\"");
                    table.ForeignKey(
                        name: "FK_UserBlocks_Users_BlockedUserId",
                        column: x => x.BlockedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBlocks_Users_BlockerUserId",
                        column: x => x.BlockerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedUserId",
                table: "UserBlocks",
                column: "BlockedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerUserId_BlockedUserId",
                table: "UserBlocks",
                columns: new[] { "BlockerUserId", "BlockedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerUserId_CreatedAt",
                table: "UserBlocks",
                columns: new[] { "BlockerUserId", "CreatedAt" });

            migrationBuilder.Sql(
                """
                INSERT INTO "UserBlocks" (
                    "Id",
                    "BlockerUserId",
                    "BlockedUserId",
                    "CreatedAt",
                    "UpdatedAt",
                    "CreatedByUserId",
                    "UpdatedByUserId")
                SELECT
                    gen_random_uuid(),
                    COALESCE("BlockedByUserId", "RequesterUserId"),
                    CASE
                        WHEN COALESCE("BlockedByUserId", "RequesterUserId") = "RequesterUserId"
                            THEN "AddresseeUserId"
                        ELSE "RequesterUserId"
                    END,
                    COALESCE("RespondedAt", "CreatedAt"),
                    "UpdatedAt",
                    "CreatedByUserId",
                    "UpdatedByUserId"
                FROM "Friendships"
                WHERE "Status" = 3
                ON CONFLICT ("BlockerUserId", "BlockedUserId") DO NOTHING;

                DELETE FROM "Friendships" WHERE "Status" = 3;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Friendships_Users_BlockedByUserId",
                table: "Friendships");

            migrationBuilder.DropIndex(
                name: "IX_Friendships_BlockedByUserId",
                table: "Friendships");

            migrationBuilder.DropColumn(
                name: "BlockedByUserId",
                table: "Friendships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBlocks");

            migrationBuilder.AddColumn<Guid>(
                name: "BlockedByUserId",
                table: "Friendships",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_BlockedByUserId",
                table: "Friendships",
                column: "BlockedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Friendships_Users_BlockedByUserId",
                table: "Friendships",
                column: "BlockedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
