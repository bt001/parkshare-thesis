using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class CreateBookingCommand : IRequest<ServiceResult<CreateBookingResponse>>
    {
        public long ListingId { get; init; }
        public long RenterId { get; init; }
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
    }

    public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, ServiceResult<CreateBookingResponse>>
    {
        private readonly IBookingService _bookingService;
        private readonly IListingReadService _listingReadService;
        private readonly IAvailabilityService _availabilityService;

        public CreateBookingHandler(IBookingService bookingService, IListingReadService listingReadService, IAvailabilityService availabilityService)
          => (_bookingService, _listingReadService, _availabilityService) = (bookingService ?? throw new ArgumentNullException(nameof(bookingService)), listingReadService ?? throw new ArgumentNullException(nameof(listingReadService)), availabilityService ?? throw new ArgumentNullException(nameof(availabilityService)));

        public async Task<ServiceResult<CreateBookingResponse>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var listingRes = await _listingReadService.GetCoreAsync(request.ListingId, cancellationToken);
            if (!listingRes.IsSuccessful)
                return ServiceResult<CreateBookingResponse>.WithErrors(listingRes.Errors);

            var listing = listingRes.Payload;
            if (!listing.IsActive)
                return ServiceResult<CreateBookingResponse>.WithError("Listing is not active.");

            if (listing.OwnerId == request.RenterId)
                return ServiceResult<CreateBookingResponse>.WithError("Owner cannot book own listing.");

            if (!listing.IsAlwaysAvailable)
            {
                var coverage = await _availabilityService.HasWindowCoverageAsync(request.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
                if (!coverage.IsSuccessful)
                    return ServiceResult<CreateBookingResponse>.WithErrors(coverage.Errors);
                if (!coverage.Payload)
                    return ServiceResult<CreateBookingResponse>.WithError("Requested time is outside available windows.");
            }

            var blocked = await _availabilityService.HasBlockedOverlapAsync(request.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
            if (!blocked.IsSuccessful)
                return ServiceResult<CreateBookingResponse>.WithErrors(blocked.Errors);
            if (blocked.Payload)
                return ServiceResult<CreateBookingResponse>.WithError("Requested time overlaps a blocked window.");

            var confirmedOverlap = await _bookingService.HasConfirmedOverlapAsync(request.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
            if (!confirmedOverlap.IsSuccessful)
                return ServiceResult<CreateBookingResponse>.WithErrors(confirmedOverlap.Errors);
            if (confirmedOverlap.Payload)
                return ServiceResult<CreateBookingResponse>.WithError("Requested time overlaps a confirmed booking.");

            var create = await _bookingService.CreateAsync(request, cancellationToken);
            if (!create.IsSuccessful)
                return ServiceResult<CreateBookingResponse>.WithErrors(create.Errors);

            return ServiceResult<CreateBookingResponse>.Success(create.Payload);
        }
    }

}
