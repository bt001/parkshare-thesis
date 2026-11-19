using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkShare.Infrastructure.Migrations;

/// <summary>Adds recipient read tracking while treating historical messages as already read.</summary>
public partial class ChatReadState : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(name: "ReadAt", table: "ChatMessages", type: "datetime2", nullable: true);
        migrationBuilder.Sql("UPDATE ChatMessages SET ReadAt = SYSUTCDATETIME() WHERE ReadAt IS NULL;");
        migrationBuilder.CreateIndex(name: "IX_ChatMessages_BookingId_SenderId", table: "ChatMessages", columns: new[] { "BookingId", "SenderId" }, filter: "[ReadAt] IS NULL");
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_ChatMessages_BookingId_SenderId", table: "ChatMessages");
        migrationBuilder.DropColumn(name: "ReadAt", table: "ChatMessages");
    }
}
