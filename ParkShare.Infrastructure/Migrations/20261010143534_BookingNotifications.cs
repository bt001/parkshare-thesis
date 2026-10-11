using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<long>(
                name: "BookingId",
                table: "Notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BookingId",
                table: "Notifications",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_BookingId_Type",
                table: "Notifications",
                columns: new[] { "UserId", "BookingId", "Type" },
                unique: true,
                filter: "[BookingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_CreatedAt_Id",
                table: "Notifications",
                columns: new[] { "UserId", "CreatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Bookings_BookingId",
                table: "Notifications",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Bookings_BookingId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_BookingId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_BookingId_Type",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_CreatedAt_Id",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "BookingId",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");
        }
    }
}
