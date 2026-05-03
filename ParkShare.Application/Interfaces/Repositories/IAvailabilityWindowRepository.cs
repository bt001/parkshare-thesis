using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides persistence operations for the <see cref="AvailabilityWindow"/> table.
    /// </summary>
    public interface IAvailabilityWindowRepository
    {
        /// <summary>
        /// Creates an availability window row.
        /// </summary>
        Task AddAsync(AvailabilityWindow availabilityWindow, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a tracked availability window by ID.
        /// </summary>
        Task<AvailabilityWindow?> GetByIdAsync(long availabilityWindowId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a detached availability window by ID with its listing loaded.
        /// </summary>
        Task<AvailabilityWindow?> GetByIdWithListingAsync(long availabilityWindowId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns availability windows for a listing with optional date filtering.
        /// </summary>
        Task<IReadOnlyList<AvailabilityWindow>> GetByListingAsync(long listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken cancellationToken);

        /// <summary>
        /// Returns availability windows for an owner with optional filters.
        /// </summary>
        Task<IReadOnlyList<AvailabilityWindow>> GetByOwnerAsync(long ownerId, long? listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken cancellationToken);

        /// <summary>
        /// Returns open availability windows that overlap the requested time range.
        /// </summary>
        Task<IReadOnlyList<AvailabilityWindow>> GetOpenOverlappingWindowsAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether a blocked window overlaps the requested time range.
        /// </summary>
        Task<bool> HasBlockedOverlapAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken);

        /// <summary>
        /// Persists tracked availability window changes.
        /// </summary>
        Task SaveChangesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Deletes a tracked availability window row.
        /// </summary>
        Task DeleteAsync(AvailabilityWindow availabilityWindow, CancellationToken cancellationToken);
    }
}
