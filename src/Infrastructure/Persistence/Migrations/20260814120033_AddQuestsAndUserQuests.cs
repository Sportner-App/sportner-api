using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestsAndUserQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MetricCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetValue = table.Column<int>(type: "integer", nullable: false),
                    RewardBadgeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<short>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Quests_Badges_RewardBadgeId",
                        column: x => x.RewardBadgeId,
                        principalTable: "Badges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserQuests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CurrentValue = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserQuests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserQuests_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserQuests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quests_Code",
                table: "Quests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Quests_IsActive",
                table: "Quests",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_MetricCode",
                table: "Quests",
                column: "MetricCode");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_RewardBadgeId",
                table: "Quests",
                column: "RewardBadgeId");

            migrationBuilder.CreateIndex(
                name: "IX_Quests_SortOrder",
                table: "Quests",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuests_QuestId",
                table: "UserQuests",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuests_Status",
                table: "UserQuests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuests_UserId",
                table: "UserQuests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserQuests_UserId_QuestId",
                table: "UserQuests",
                columns: new[] { "UserId", "QuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserQuests_UserId_Status",
                table: "UserQuests",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserQuests");

            migrationBuilder.DropTable(
                name: "Quests");
        }
    }
}
