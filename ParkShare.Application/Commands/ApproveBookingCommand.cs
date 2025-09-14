using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Enums;

namespace ParkShare.Application.Commands
{
    public class ApproveBookingCommand : IRequest<ServiceResult<ApproveBookingResponse>>
    {
        public long BookingId { get; init; }
        public long OwnerId { get; init; }
    }

    public class ApproveBookingHandler : IRequestHandler<ApproveBookingCommand, ServiceResult<ApproveBookingResponse>>
    {
        private readonly IBookingService _bookingService;
        private readonly IListingReadService _listingReadService;
        private readonly IAvailabilityService _availabilityService;

        public ApproveBookingHandler(IBookingService bookingService, IListingReadService listingReadService, IAvailabilityService availabilityService)
          => (_bookingService, _listingReadService, _availabilityService) = (bookingService ?? throw new ArgumentNullException(nameof(bookingService)), listingReadService ?? throw new ArgumentNullException(nameof(listingReadService)), availabilityService ?? throw new ArgumentNullException(nameof(availabilityService)));

        public async Task<ServiceResult<ApproveBookingResponse>> Handle(ApproveBookingCommand request, CancellationToken cancellationToken)
        {
            var ownerRes = await _bookingService.GetOwnerForBookingAsync(request.BookingId, cancellationToken);
            if (!ownerRes.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(ownerRes.Errors);
            if (ownerRes.Payload != request.OwnerId)
                return ServiceResult<ApproveBookingResponse>.WithError("Not authorized to approve this booking.");

            var bookingRes = await _bookingService.GetByIdAsync(request.BookingId, cancellationToken);
            if (!bookingRes.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(bookingRes.Errors);

            var b = bookingRes.Payload;
            if (b.Status == BookingStatus.Confirmed)
                return ServiceResult<ApproveBookingResponse>.Success(new ApproveBookingResponse
                {
                    BookingId = b.BookingId,
                    ListingId = b.ListingId,
                    RenterId = b.RenterId,
                    OwnerId = b.OwnerId,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                });

            var listingRes = await _listingReadService.GetCoreAsync(b.ListingId, cancellationToken);
            if (!listingRes.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(listingRes.Errors);

            var listing = listingRes.Payload;
            if (!listing.IsActive)
                return ServiceResult<ApproveBookingResponse>.WithError("Listing is not active.");

            if (!listing.IsAlwaysAvailable)
            {
                var coverage = await _availabilityService.HasWindowCoverageAsync(b.ListingId, b.StartDateTime, b.EndDateTime, cancellationToken);
                if (!coverage.IsSuccessful)
                    return ServiceResult<ApproveBookingResponse>.WithErrors(coverage.Errors);
                if (!coverage.Payload)
                    return ServiceResult<ApproveBookingResponse>.WithError("Requested time is outside available windows.");
            }

            var blocked = await _availabilityService.HasBlockedOverlapAsync(b.ListingId, b.StartDateTime, b.EndDateTime, cancellationToken);
            if (!blocked.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(blocked.Errors);
            if (blocked.Payload)
                return ServiceResult<ApproveBookingResponse>.WithError("Requested time overlaps a blocked window.");

            var confirmedOverlap = await _bookingService.HasConfirmedOverlapAsync(b.ListingId, b.StartDateTime, b.EndDateTime, cancellationToken);
            if (!confirmedOverlap.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(confirmedOverlap.Errors);
            if (confirmedOverlap.Payload)
                return ServiceResult<ApproveBookingResponse>.WithError("Requested time overlaps a confirmed booking.");

            var approve = await _bookingService.ApproveAndRejectOverlappingPendingAsync(request.BookingId, cancellationToken);
            if (!approve.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(approve.Errors);

            return ServiceResult<ApproveBookingResponse>.Success(approve.Payload);
        }
    }

}
