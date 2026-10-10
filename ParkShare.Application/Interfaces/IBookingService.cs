using ParkShare.Application.Commands;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Common.Enums;

namespace ParkShare.Application.Interfaces
{
    /// <summary>Coordinates booking queries and serialized, validated booking mutations.</summary>
    public interface IBookingService
    {
        /// <summary>Returns renter bookings filtered by status, including inactive listings.</summary>
        Task<ServiceResult<GetRenterBookingsResponse>> GetRenterByStatusesAsync(long renterId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken cancellationToken);

        /// <summary>Checks for confirmed overlap.</summary>
        Task<ServiceResult<bool>> HasConfirmedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct);

        /// <summary>Validates and creates a Pending request under the listing lock.</summary>
        Task<ServiceResult<CreateBookingResponse>> CreateAsync(CreateBookingCommand cmd, CancellationToken ct);

        /// <summary>Returns booking details.</summary>
        Task<ServiceResult<GetBookingByIdResponse>> GetByIdAsync(long bookingId, CancellationToken ct);

        /// <summary>Returns the owner identity for a booking.</summary>
        Task<ServiceResult<long>> GetOwnerForBookingAsync(long bookingId, CancellationToken ct);

        /// <summary>Validates owner identity and atomically approves a Pending request and rejects overlaps.</summary>
        Task<ServiceResult<ApproveBookingResponse>> ApproveAndRejectOverlappingPendingAsync(long bookingId, long ownerId, CancellationToken ct);

        /// <summary>Validates owner identity and rejects a Pending request under the listing lock.</summary>
        Task<ServiceResult<RejectBookingResponse>> RejectAsync(long bookingId, long ownerId, string? reason, CancellationToken ct);

        /// <summary>Returns paged owner booking results.</summary>
        Task<ServiceResult<GetOwnerBookingsResponse>> GetOwnerByStatusesAsync(long ownerId, long? listingId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken ct);
    }
}
