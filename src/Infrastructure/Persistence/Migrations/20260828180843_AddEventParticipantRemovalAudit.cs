using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventParticipantRemovalAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventParticipantRemovals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RemovedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportReasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventParticipantRemovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventParticipantRemovals_EventParticipants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "EventParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventParticipantRemovals_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventParticipantRemovals_ReportReasons_ReportReasonId",
                        column: x => x.ReportReasonId,
                        principalTable: "ReportReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventParticipantRemovals_Users_OrganizerUserId",
                        column: x => x.OrganizerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventParticipantRemovals_Users_RemovedUserId",
                        column: x => x.RemovedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipantRemovals_CreatedAt",
                table: "EventParticipantRemovals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipantRemovals_EventId",
                table: "EventParticipantRemovals",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipantRemovals_OrganizerUserId",
                table: "EventParticipantRemovals",
                column: "OrganizerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipantRemovals_ParticipantId",
                table: "EventParticipantRemovals",
                column: "ParticipantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipantRemovals_RemovedUserId",
                table: "EventParticipantRemovals",
                column: "RemovedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipantRemovals_ReportReasonId",
                table: "EventParticipantRemovals",
                column: "ReportReasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventParticipantRemovals");
        }
    }
}
