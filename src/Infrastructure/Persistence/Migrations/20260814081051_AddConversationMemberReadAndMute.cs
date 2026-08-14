using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationMemberReadAndMute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastReadAt",
                table: "ConversationMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastReadMessageId",
                table: "ConversationMembers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MutedUntil",
                table: "ConversationMembers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_LastReadMessageId",
                table: "ConversationMembers",
                column: "LastReadMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMembers_Messages_LastReadMessageId",
                table: "ConversationMembers",
                column: "LastReadMessageId",
                principalTable: "Messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMembers_Messages_LastReadMessageId",
                table: "ConversationMembers");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMembers_LastReadMessageId",
                table: "ConversationMembers");

            migrationBuilder.DropColumn(
                name: "LastReadAt",
                table: "ConversationMembers");

            migrationBuilder.DropColumn(
                name: "LastReadMessageId",
                table: "ConversationMembers");

            migrationBuilder.DropColumn(
                name: "MutedUntil",
                table: "ConversationMembers");
        }
    }
}
