using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides persistence operations for the <see cref="Listing"/> table.
    /// </summary>
    public interface IListingRepository
    {
        /// <summary>Locks a listing until the current transaction ends. Call before reading mutable booking state.</summary>
        Task LockAsync(long listingId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a listing row.
        /// </summary>
        /// <param name="listing">Listing entity.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task AddAsync(Listing listing, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a tracked listing by ID.
        /// </summary>
        /// <param name="listingId">Listing ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching listing when found.</returns>
        Task<Listing?> GetByIdAsync(long listingId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a detached listing by ID.
        /// </summary>
        /// <param name="listingId">Listing ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Matching listing when found.</returns>
        Task<Listing?> GetByIdAsNoTrackingAsync(long listingId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns owner listings ordered by newest first.
        /// </summary>
        /// <param name="ownerId">Owner ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Owner listings.</returns>
        Task<IReadOnlyList<Listing>> GetByOwnerAsync(long ownerId, CancellationToken cancellationToken);

        /// <summary>
        /// Searches listings using the provided filters.
        /// </summary>
        Task<PagedResult<ListingSearchRow>> SearchAsync(
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
            CancellationToken cancellationToken, DateTime? startDateTime = null, DateTime? endDateTime = null, IReadOnlyCollection<long>? amenityIds = null, double? minRating = null, string? sortBy = null, double? south = null, double? north = null, double? west = null, double? east = null);

        /// <summary>
        /// Persists tracked listing changes.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Completion task.</returns>
        Task SaveChangesAsync(CancellationToken cancellationToken);
    }
}
