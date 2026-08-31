using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCommentReplyToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReplyToUserId",
                table: "PostComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostComments_ReplyToUserId",
                table: "PostComments",
                column: "ReplyToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PostComments_Users_ReplyToUserId",
                table: "PostComments",
                column: "ReplyToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PostComments_Users_ReplyToUserId",
                table: "PostComments");

            migrationBuilder.DropIndex(
                name: "IX_PostComments_ReplyToUserId",
                table: "PostComments");

            migrationBuilder.DropColumn(
                name: "ReplyToUserId",
                table: "PostComments");
        }
    }
}
