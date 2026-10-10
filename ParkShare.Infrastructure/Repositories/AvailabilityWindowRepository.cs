using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class AvailabilityWindowRepository : IAvailabilityWindowRepository
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the availability window repository.
        /// </summary>
        /// <param name="db">Database context.</param>
        public AvailabilityWindowRepository(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public Task<long?> GetListingIdAsync(long availabilityWindowId, CancellationToken cancellationToken)
            => _db.AvailabilityWindows.AsNoTracking().Where(w => w.Id == availabilityWindowId)
                .Select(w => (long?)w.ListingId).FirstOrDefaultAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(AvailabilityWindow availabilityWindow, CancellationToken cancellationToken)
        {
            await _db.AvailabilityWindows.AddAsync(availabilityWindow, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<AvailabilityWindow?> GetByIdAsync(long availabilityWindowId, CancellationToken cancellationToken)
            => _db.AvailabilityWindows.FirstOrDefaultAsync(window => window.Id == availabilityWindowId, cancellationToken);

        /// <inheritdoc/>
        public Task<AvailabilityWindow?> GetByIdWithListingAsync(long availabilityWindowId, CancellationToken cancellationToken)
            => _db.AvailabilityWindows
                .AsNoTracking()
                .Include(window => window.Listing)
                .FirstOrDefaultAsync(window => window.Id == availabilityWindowId, cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<AvailabilityWindow>> GetByListingAsync(long listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken cancellationToken)
            => await BuildFilteredQuery(startDateTime, endDateTime)
                .Where(window => window.ListingId == listingId)
                .OrderBy(window => window.StartDateTime)
                .ThenBy(window => window.EndDateTime)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<IReadOnlyList<AvailabilityWindow>> GetByOwnerAsync(long ownerId, long? listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken cancellationToken)
        {
            var query = BuildFilteredQuery(startDateTime, endDateTime)
                .Where(window => window.Listing.OwnerId == ownerId);

            if (listingId.HasValue)
                query = query.Where(window => window.ListingId == listingId.Value);

            return await query
                .OrderBy(window => window.ListingId)
                .ThenBy(window => window.StartDateTime)
                .ThenBy(window => window.EndDateTime)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<AvailabilityWindow>> GetOpenOverlappingWindowsAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken)
            => await _db.AvailabilityWindows
                .AsNoTracking()
                .Where(window => window.ListingId == listingId
                    && !window.IsBlocked
                    && window.EndDateTime > startDateTime
                    && window.StartDateTime < endDateTime)
                .OrderBy(window => window.StartDateTime)
                .ToListAsync(cancellationToken);

        /// <inheritdoc/>
        public Task<bool> HasBlockedOverlapAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken)
            => _db.AvailabilityWindows
                .AsNoTracking()
                .AnyAsync(window => window.ListingId == listingId
                    && window.IsBlocked
                    && window.EndDateTime > startDateTime
                    && window.StartDateTime < endDateTime, cancellationToken);

        /// <inheritdoc/>
        public Task SaveChangesAsync(CancellationToken cancellationToken)
            => _db.SaveChangesAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task DeleteAsync(AvailabilityWindow availabilityWindow, CancellationToken cancellationToken)
        {
            _db.AvailabilityWindows.Remove(availabilityWindow);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Builds the base availability query with optional date filtering.
        /// </summary>
        private IQueryable<AvailabilityWindow> BuildFilteredQuery(DateTime? startDateTime, DateTime? endDateTime)
        {
            var query = _db.AvailabilityWindows
                .AsNoTracking()
                .Include(window => window.Listing)
                .AsQueryable();

            if (startDateTime.HasValue)
                query = query.Where(window => window.EndDateTime > startDateTime.Value);

            if (endDateTime.HasValue)
                query = query.Where(window => window.StartDateTime < endDateTime.Value);

            return query;
        }
    }
}
