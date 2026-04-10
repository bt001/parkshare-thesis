using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Enums;

namespace ParkShare.Application.Queries
{
    public class GetOwnerBookingsQuery : IRequest<ServiceResult<GetOwnerBookingsResponse>>
    {
        public long OwnerId { get; init; }
        public long? ListingId { get; init; }
        public List<BookingStatus>? Statuses { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    public class GetOwnerBookingsHandler : IRequestHandler<GetOwnerBookingsQuery, ServiceResult<GetOwnerBookingsResponse>>
    {
        private readonly IBookingService _bookingService;
        private readonly IListingReadService _listingReadService;

        public GetOwnerBookingsHandler(IBookingService bookingService, IListingReadService listingReadService)
          => (_bookingService, _listingReadService) = (bookingService ?? throw new ArgumentNullException(nameof(bookingService)), listingReadService ?? throw new ArgumentNullException(nameof(listingReadService)));

        public async Task<ServiceResult<GetOwnerBookingsResponse>> Handle(GetOwnerBookingsQuery request, CancellationToken cancellationToken)
        {
            if (request.ListingId.HasValue)
            {
                var core = await _listingReadService.GetCoreAsync(request.ListingId.Value, cancellationToken);
                if (!core.IsSuccessful)
                    return ServiceResult<GetOwnerBookingsResponse>.WithErrors(core.Errors);

                if (core.Payload.OwnerId != request.OwnerId)
                    return ServiceResult<GetOwnerBookingsResponse>.WithError("Listing does not belong to the specified owner.");
            }

            var response = await _bookingService.GetOwnerByStatusesAsync(request.OwnerId, request.ListingId, request.Statuses, request.Page, request.PageSize, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<GetOwnerBookingsResponse>.WithErrors(response.Errors);

            return ServiceResult<GetOwnerBookingsResponse>.Success(response.Payload);
        }
    }
}
