using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class AmenityRepository : IAmenityRepository
    {
        private readonly ParkShareDbContext _db;
        /// <summary>Creates a catalog repository.</summary>
        public AmenityRepository(ParkShareDbContext db) => _db = db;
        /// <inheritdoc/>
        public async Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken ct)
            => await _db.Amenities.AsNoTracking().OrderBy(a => a.Name).ThenBy(a => a.Id).ToListAsync(ct);
    }
}
