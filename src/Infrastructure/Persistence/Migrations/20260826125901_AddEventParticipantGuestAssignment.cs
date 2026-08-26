using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventParticipantGuestAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_EventId_UserId",
                table: "EventParticipants");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "EventParticipants",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "GuestFirstName",
                table: "EventParticipants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestLastName",
                table: "EventParticipants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "Kind",
                table: "EventParticipants",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId_UserId",
                table: "EventParticipants",
                columns: new[] { "EventId", "UserId" },
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_Kind",
                table: "EventParticipants",
                column: "Kind");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_EventId_UserId",
                table: "EventParticipants");

            migrationBuilder.DropIndex(
                name: "IX_EventParticipants_Kind",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "GuestFirstName",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "GuestLastName",
                table: "EventParticipants");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "EventParticipants");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "EventParticipants",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId_UserId",
                table: "EventParticipants",
                columns: new[] { "EventId", "UserId" },
                unique: true);
        }
    }
}
