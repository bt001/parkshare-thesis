using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>
    /// Provides persistence operations for the <see cref="Booking"/> table.
    /// </summary>
    public interface IBookingRepository
    {
        /// <summary>Returns renter bookings filtered by status, including inactive listings.</summary>
        Task<PagedResult<Booking>> GetRenterByStatusesAsync(long renterId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>
        /// Checks whether a confirmed booking overlaps the requested time range.
        /// </summary>
        Task<bool> HasConfirmedOverlapAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a booking row.
        /// </summary>
        Task AddAsync(Booking booking, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a booking by ID with its listing loaded.
        /// </summary>
        Task<Booking?> GetByIdWithListingAsync(long bookingId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the owner ID for a booking.
        /// </summary>
        Task<long?> GetOwnerIdAsync(long bookingId, CancellationToken cancellationToken);

        /// <summary>
        /// Confirms a booking and rejects overlapping pending bookings atomically.
        /// </summary>
        Task<Booking?> ApproveAndRejectOverlappingPendingAsync(long bookingId, CancellationToken cancellationToken);

        /// <summary>
        /// Rejects a booking.
        /// </summary>
        Task<Booking?> RejectAsync(long bookingId, CancellationToken cancellationToken);

        /// <summary>
        /// Returns owner bookings filtered by listing and status.
        /// </summary>
        Task<PagedResult<Booking>> GetOwnerByStatusesAsync(long ownerId, long? listingId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken cancellationToken);
    }
}
