using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Domain.Entities.JoinModels;
using ParkShare.Infrastructure.Persistence;
namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class ListingAmenityRepository : IListingAmenityRepository
    {
        private readonly ParkShareDbContext _db;
        /// <summary>Creates a listing selection repository.</summary>
        public ListingAmenityRepository(ParkShareDbContext db) => _db = db;
        /// <inheritdoc/>
        public async Task<IReadOnlyList<Amenity>> GetByListingAsync(long listingId, CancellationToken ct)
            => await _db.ListingAmenities.AsNoTracking().Where(a => a.ListingId == listingId)
                .Select(a => a.Amenity).OrderBy(a => a.Name).ThenBy(a => a.Id).ToListAsync(ct);
        /// <inheritdoc/>
        public async Task ReplaceAsync(long listingId, IReadOnlyCollection<long> amenityIds, CancellationToken ct)
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Amenity replacement requires a listing transaction.");
            var existing = await _db.ListingAmenities.Where(a => a.ListingId == listingId).ToListAsync(ct);
            _db.ListingAmenities.RemoveRange(existing.Where(a => !amenityIds.Contains(a.AmenityId)));
            var retained = existing.Select(a => a.AmenityId).ToHashSet();
            _db.ListingAmenities.AddRange(amenityIds.Where(id => !retained.Contains(id))
                .Select(id => new ListingAmenity { ListingId = listingId, AmenityId = id }));
            await _db.SaveChangesAsync(ct);
        }
    }
}
