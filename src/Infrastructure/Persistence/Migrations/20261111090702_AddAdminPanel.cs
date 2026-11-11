using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "AppFeedbacks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RepliedAt",
                table: "AppFeedbacks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RepliedByUserId",
                table: "AppFeedbacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Status",
                table: "AppFeedbacks",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "AdminAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TargetType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdminLoginCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminLoginCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppFeedbacks_Status_CreatedAt",
                table: "AppFeedbacks",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEntries_CreatedAt",
                table: "AdminAuditEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditEntries_TargetType_TargetId",
                table: "AdminAuditEntries",
                columns: new[] { "TargetType", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminLoginCodes_UserId_CreatedAt",
                table: "AdminLoginCodes",
                columns: new[] { "UserId", "CreatedAt" });

            // Supabase'in herkese açık REST katmanından erişilmesin; API service bağlantısıyla çalışır.
            migrationBuilder.Sql("ALTER TABLE \"AdminAuditEntries\" ENABLE ROW LEVEL SECURITY;");
            migrationBuilder.Sql("ALTER TABLE \"AdminLoginCodes\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditEntries");

            migrationBuilder.DropTable(
                name: "AdminLoginCodes");

            migrationBuilder.DropIndex(
                name: "IX_AppFeedbacks_Status_CreatedAt",
                table: "AppFeedbacks");

            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "AppFeedbacks");

            migrationBuilder.DropColumn(
                name: "RepliedAt",
                table: "AppFeedbacks");

            migrationBuilder.DropColumn(
                name: "RepliedByUserId",
                table: "AppFeedbacks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AppFeedbacks");
        }
    }
}
