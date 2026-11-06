using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HashedCredentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPasswordHashed",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Credential conversion is irreversible. Restore a pre-upgrade database backup to downgrade.");
        }
    }
}
