using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class ListingRepository : IListingRepository
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the listing repository.
        /// </summary>
        /// <param name="db">Database context.</param>
        public ListingRepository(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public async Task AddAsync(Listing listing, CancellationToken cancellationToken)
        {
            await _db.Listings.AddAsync(listing, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken)
            => _db.Listings.FirstOrDefaultAsync(listing => listing.Id == listingId, cancellationToken);

        /// <inheritdoc/>
        public Task<Listing?> GetByIdAsNoTrackingAsync(long listingId, CancellationToken cancellationToken)
            => _db.Listings.AsNoTracking().FirstOrDefaultAsync(listing => listing.Id == listingId, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<Listing>> GetByOwnerAsync(long ownerId, CancellationToken cancellationToken)
            => await _db.Listings
                .AsNoTracking()
                .Where(listing => listing.OwnerId == ownerId)
                .OrderByDescending(listing => listing.CreatedAt)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<PagedResult<Listing>> SearchAsync(
            double? lat,
            double? lng,
            double? radiusKm,
            decimal? minPrice,
            decimal? maxPrice,
            bool? onlyActive,
            bool? onlyAlwaysAvailable,
            string? titleContains,
            string? addressContains,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var query = _db.Listings.AsNoTracking().AsQueryable();

            var active = onlyActive ?? true;
            if (active)
                query = query.Where(listing => listing.IsActive);

            if (onlyAlwaysAvailable == true)
                query = query.Where(listing => listing.IsAlwaysAvailable);

            if (minPrice.HasValue)
                query = query.Where(listing => listing.PricePerHour >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(listing => listing.PricePerHour <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(titleContains))
                query = query.Where(listing => EF.Functions.Like(listing.Title, $"%{titleContains}%"));

            if (!string.IsNullOrWhiteSpace(addressContains))
                query = query.Where(listing => listing.Address != null && EF.Functions.Like(listing.Address, $"%{addressContains}%"));

            if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            {
                var approxDeg = radiusKm.Value / 111.0;
                var minLat = lat.Value - approxDeg;
                var maxLat = lat.Value + approxDeg;
                var minLng = lng.Value - approxDeg;
                var maxLng = lng.Value + approxDeg;

                query = query.Where(listing =>
                    (double)listing.Latitude >= minLat &&
                    (double)listing.Latitude <= maxLat &&
                    (double)listing.Longitude >= minLng &&
                    (double)listing.Longitude <= maxLng);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(listing => listing.CreatedAt)
                .ThenByDescending(listing => listing.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Listing>(items, total);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => _db.SaveChangesAsync(cancellationToken);
    }
}
