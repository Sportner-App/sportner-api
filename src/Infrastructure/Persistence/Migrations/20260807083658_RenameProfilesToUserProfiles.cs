using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class RenameProfilesToUserProfiles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameTable(
            name: "Profiles",
            newName: "UserProfiles");

        migrationBuilder.RenameIndex(
            name: "IX_Profiles_AverageRating",
            table: "UserProfiles",
            newName: "IX_UserProfiles_AverageRating");

        migrationBuilder.RenameIndex(
            name: "IX_Profiles_City",
            table: "UserProfiles",
            newName: "IX_UserProfiles_City");

        migrationBuilder.RenameIndex(
            name: "IX_Profiles_UserId",
            table: "UserProfiles",
            newName: "IX_UserProfiles_UserId");

        migrationBuilder.RenameIndex(
            name: "IX_Profiles_Username",
            table: "UserProfiles",
            newName: "IX_UserProfiles_Username");

        migrationBuilder.Sql(
            """ALTER TABLE "UserProfiles" RENAME CONSTRAINT "PK_Profiles" TO "PK_UserProfiles";""");

        migrationBuilder.Sql(
            """ALTER TABLE "UserProfiles" RENAME CONSTRAINT "FK_Profiles_Users_UserId" TO "FK_UserProfiles_Users_UserId";""");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """ALTER TABLE "UserProfiles" RENAME CONSTRAINT "FK_UserProfiles_Users_UserId" TO "FK_Profiles_Users_UserId";""");

        migrationBuilder.Sql(
            """ALTER TABLE "UserProfiles" RENAME CONSTRAINT "PK_UserProfiles" TO "PK_Profiles";""");

        migrationBuilder.RenameIndex(
            name: "IX_UserProfiles_Username",
            table: "UserProfiles",
            newName: "IX_Profiles_Username");

        migrationBuilder.RenameIndex(
            name: "IX_UserProfiles_UserId",
            table: "UserProfiles",
            newName: "IX_Profiles_UserId");

        migrationBuilder.RenameIndex(
            name: "IX_UserProfiles_City",
            table: "UserProfiles",
            newName: "IX_Profiles_City");

        migrationBuilder.RenameIndex(
            name: "IX_UserProfiles_AverageRating",
            table: "UserProfiles",
            newName: "IX_Profiles_AverageRating");

        migrationBuilder.RenameTable(
            name: "UserProfiles",
            newName: "Profiles");
    }
}
