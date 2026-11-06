using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    /// <summary>
    /// Gets availability windows for an owner.
    /// </summary>
    public class GetOwnerAvailabilityQuery : IRequest<ServiceResult<GetAvailabilityWindowsResponse>>
    {
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; init; }
        /// <summary>
        /// Optional listing filter.
        /// </summary>
        public long? ListingId { get; init; }
        /// <summary>
        /// Optional lower bound.
        /// </summary>
        public DateTime? StartDateTime { get; init; }
        /// <summary>
        /// Optional upper bound.
        /// </summary>
        public DateTime? EndDateTime { get; init; }
    }

    /// <summary>
    /// Handles owner availability queries.
    /// </summary>
    public class GetOwnerAvailabilityHandler : IRequestHandler<GetOwnerAvailabilityQuery, ServiceResult<GetAvailabilityWindowsResponse>>
    {
        private readonly IAvailabilityService _availabilityService;
        private readonly IListingReadService _listingReadService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        public GetOwnerAvailabilityHandler(IAvailabilityService availabilityService, IListingReadService listingReadService)
            => (_availabilityService, _listingReadService) = (
                availabilityService ?? throw new ArgumentNullException(nameof(availabilityService)),
                listingReadService ?? throw new ArgumentNullException(nameof(listingReadService)));

        /// <summary>
        /// Handles the query.
        /// </summary>
        public async Task<ServiceResult<GetAvailabilityWindowsResponse>> Handle(GetOwnerAvailabilityQuery request, CancellationToken cancellationToken)
        {
            if (request.ListingId.HasValue)
            {
                var listingRes = await _listingReadService.GetCoreAsync(request.ListingId.Value, cancellationToken);
                if (!listingRes.IsSuccessful)
                    return ServiceResult<GetAvailabilityWindowsResponse>.WithErrors(listingRes);

                if (listingRes.Payload.OwnerId != request.OwnerId)
                    return ServiceResult<GetAvailabilityWindowsResponse>.WithError("Listing does not belong to the specified owner.");
            }

            var response = await _availabilityService.GetOwnerAvailabilityAsync(request.OwnerId, request.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<GetAvailabilityWindowsResponse>.WithErrors(response);

            return ServiceResult<GetAvailabilityWindowsResponse>.Success(response.Payload);
        }
    }
}
