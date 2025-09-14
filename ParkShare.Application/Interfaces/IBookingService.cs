using ParkShare.Application.Commands;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Common.Enums;

namespace ParkShare.Application.Interfaces
{
    public interface IBookingService
    {
        Task<ServiceResult<bool>> HasConfirmedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct);

        Task<ServiceResult<CreateBookingResponse>> CreateAsync(CreateBookingCommand cmd, CancellationToken ct);

        Task<ServiceResult<GetBookingByIdResponse>> GetByIdAsync(long bookingId, CancellationToken ct);

        Task<ServiceResult<long>> GetOwnerForBookingAsync(long bookingId, CancellationToken ct);

        Task<ServiceResult<ApproveBookingResponse>> ApproveAndRejectOverlappingPendingAsync(long bookingId, CancellationToken ct);

        Task<ServiceResult<RejectBookingResponse>> RejectAsync(long bookingId, string? reason, CancellationToken ct);

        Task<ServiceResult<GetOwnerBookingsResponse>> GetOwnerByStatusesAsync(long ownerId, long? listingId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken ct);
    }
}
