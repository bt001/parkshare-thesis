using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkShare.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeUserRoleCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE u
                SET Role =
                    CASE
                        WHEN EXISTS (SELECT 1 FROM Listings l WHERE l.OwnerId = u.Id)
                            THEN CASE WHEN u.Role LIKE '%Admin%' THEN 'Renter,Owner,Admin' ELSE 'Renter,Owner' END
                        ELSE CASE WHEN u.Role LIKE '%Admin%' THEN 'Renter,Admin' ELSE 'Renter' END
                    END
                FROM Users u;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE u
                SET Role =
                    CASE
                        WHEN u.Role LIKE '%Admin%' THEN 'Admin'
                        WHEN u.Role LIKE '%Owner%' THEN 'Owner'
                        ELSE 'Renter'
                    END
                FROM Users u;
                """);
        }
    }
}
