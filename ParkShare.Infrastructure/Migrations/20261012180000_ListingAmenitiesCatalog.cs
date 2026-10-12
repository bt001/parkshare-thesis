using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ParkShare.Infrastructure.Persistence;
namespace ParkShare.Infrastructure.Migrations
{
    /// <summary>Adds missing default amenities without changing existing catalog rows or selections.</summary>
    [DbContext(typeof(ParkShareDbContext))]
    [Migration("20261012180000_ListingAmenitiesCatalog")]
    public class ListingAmenitiesCatalog : Migration
    {
        /// <inheritdoc/>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO Amenities (Name, Description)
                SELECT catalog.Name, catalog.Description
                FROM (VALUES
                    (N'Covered parking', N'Parking space protected by a roof.'),
                    (N'EV charging', N'Electric vehicle charging is available.'),
                    (N'CCTV', N'Parking area monitored by cameras.'),
                    (N'Gated access', N'Access through a gate.'),
                    (N'Lighting', N'Parking area has lighting.')
                ) AS catalog(Name, Description)
                WHERE NOT EXISTS (SELECT 1 FROM Amenities WITH (UPDLOCK, HOLDLOCK) WHERE Name = catalog.Name);
                """);
        }
        /// <summary>Retains catalog data because it may have acquired owner selections.</summary>
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
