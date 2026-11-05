using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPushDeliveryTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PushDeliveryTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserDeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketId = table.Column<string>(type: "text", nullable: false),
                    PushToken = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CheckAfter = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CheckedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ErrorCode = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PushDeliveryTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PushDeliveryTickets_NotificationDeliveryOutbox_OutboxId",
                        column: x => x.OutboxId,
                        principalTable: "NotificationDeliveryOutbox",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PushDeliveryTickets_UserDevices_UserDeviceId",
                        column: x => x.UserDeviceId,
                        principalTable: "UserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PushDeliveryTickets_OutboxId",
                table: "PushDeliveryTickets",
                column: "OutboxId");

            migrationBuilder.CreateIndex(
                name: "IX_PushDeliveryTickets_Status_CheckAfter",
                table: "PushDeliveryTickets",
                columns: new[] { "Status", "CheckAfter" });

            migrationBuilder.CreateIndex(
                name: "IX_PushDeliveryTickets_TicketId",
                table: "PushDeliveryTickets",
                column: "TicketId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PushDeliveryTickets_UserDeviceId",
                table: "PushDeliveryTickets",
                column: "UserDeviceId");

            migrationBuilder.Sql("ALTER TABLE \"PushDeliveryTickets\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PushDeliveryTickets");
        }
    }
}
