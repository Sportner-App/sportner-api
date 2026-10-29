using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sportner.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeEventDurationOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DurationMinutes",
                table: "Events",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Süresiz etkinlikler, bitiş hesabında kullanılan varsayılan süreyle geri döner.
            migrationBuilder.Sql(
                "UPDATE \"Events\" SET \"DurationMinutes\" = 60 WHERE \"DurationMinutes\" IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "DurationMinutes",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 60,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
