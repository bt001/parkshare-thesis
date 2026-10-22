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
        public async Task LockAsync(long listingId, CancellationToken cancellationToken)
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException("A listing lock requires an active transaction.");

            await _db.Listings.FromSqlInterpolated($"SELECT * FROM [Listings] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {listingId}")
                .AsNoTracking().ToListAsync(cancellationToken);
        }

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
        public async Task<PagedResult<ListingSearchRow>> SearchAsync(
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
            CancellationToken cancellationToken, DateTime? startDateTime = null, DateTime? endDateTime = null, IReadOnlyCollection<long>? amenityIds = null, double? minRating = null, string? sortBy = null, double? south = null, double? north = null, double? west = null, double? east = null)
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

            if (south.HasValue && north.HasValue && west.HasValue && east.HasValue)
            {
                var bottom = (decimal)south.Value;
                var top = (decimal)north.Value;
                var left = (decimal)west.Value;
                var right = (decimal)east.Value;
                query = query.Where(listing => listing.Latitude >= bottom && listing.Latitude <= top);
                query = left < right
                    ? query.Where(listing => listing.Longitude >= left && listing.Longitude <= right)
                    : query.Where(listing => listing.Longitude >= left || listing.Longitude <= right);
            }

            if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            {
                const double radians = Math.PI / 180;
                var latitude = lat.Value * radians;
                var longitude = lng.Value * radians;
                var limit = Math.Pow(Math.Sin(radiusKm.Value / (2 * 6371.0088)), 2);
                query = query.Where(listing =>
                    Math.Pow(Math.Sin(((double)listing.Latitude * radians - latitude) / 2), 2)
                    + Math.Cos(latitude) * Math.Cos((double)listing.Latitude * radians)
                    * Math.Pow(Math.Sin(((double)listing.Longitude * radians - longitude) / 2), 2) <= limit);
            }

            if (startDateTime.HasValue && endDateTime.HasValue)
            {
                var start = startDateTime.Value;
                var end = endDateTime.Value;
                // Full union coverage starts at arrival and has no uncovered window end before departure.
                query = query.Where(listing =>
                    !listing.AvailabilityWindows.Any(w => w.IsBlocked && w.StartDateTime < end && w.EndDateTime > start)
                    && !listing.Bookings.Any(b => b.Status == ParkShare.Common.Enums.BookingStatus.Confirmed
                        && b.StartDateTime < end && b.EndDateTime > start)
                    && (listing.IsAlwaysAvailable || (
                        listing.AvailabilityWindows.Any(w => !w.IsBlocked && w.StartDateTime <= start && w.EndDateTime > start)
                        && !listing.AvailabilityWindows.Any(w => !w.IsBlocked && w.EndDateTime > start && w.EndDateTime < end
                            && !listing.AvailabilityWindows.Any(next => !next.IsBlocked
                                && next.StartDateTime <= w.EndDateTime && next.EndDateTime > w.EndDateTime)))));
            }

            foreach (var amenityId in amenityIds?.Distinct() ?? Enumerable.Empty<long>())
                query = query.Where(listing => listing.ListingAmenities.Any(a => a.AmenityId == amenityId));
            if (minRating.HasValue)
                query = query.Where(listing => listing.Ratings.Average(r => (double?)r.Stars) >= minRating.Value);

            var total = await query.CountAsync(cancellationToken);
            var ordered = sortBy switch
            {
                "priceAsc" => query.OrderBy(l => l.PricePerHour).ThenByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id),
                "priceDesc" => query.OrderByDescending(l => l.PricePerHour).ThenByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id),
                "ratingDesc" => query.OrderByDescending(l => l.Ratings.Average(r => (double?)r.Stars)).ThenByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id),
                _ => query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            };
            var items = await ordered
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new ListingSearchRow
                {
                    Id = l.Id, Title = l.Title, Address = l.Address, Latitude = l.Latitude,
                    Longitude = l.Longitude, PricePerHour = l.PricePerHour, IsActive = l.IsActive,
                    IsAlwaysAvailable = l.IsAlwaysAvailable, CreatedAt = l.CreatedAt,
                    CoverPhotoId = l.Photos.OrderBy(p => p.UploadedAt).ThenBy(p => p.Id).Select(p => (long?)p.Id).FirstOrDefault(),
                    AverageRating = l.Ratings.Average(r => (double?)r.Stars), RatingCount = l.Ratings.Count()
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<ListingSearchRow>(items, total);
        }

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => _db.SaveChangesAsync(cancellationToken);
    }
}
