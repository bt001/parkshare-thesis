using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    /// <summary>
    /// Gets availability windows for a listing.
    /// </summary>
    public class GetListingAvailabilityQuery : IRequest<ServiceResult<GetAvailabilityWindowsResponse>>
    {
        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; init; }
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
    /// Handles listing availability queries.
    /// </summary>
    public class GetListingAvailabilityHandler : IRequestHandler<GetListingAvailabilityQuery, ServiceResult<GetAvailabilityWindowsResponse>>
    {
        private readonly IAvailabilityService _availabilityService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        public GetListingAvailabilityHandler(IAvailabilityService availabilityService)
            => _availabilityService = availabilityService ?? throw new ArgumentNullException(nameof(availabilityService));

        /// <summary>
        /// Handles the query.
        /// </summary>
        public async Task<ServiceResult<GetAvailabilityWindowsResponse>> Handle(GetListingAvailabilityQuery request, CancellationToken cancellationToken)
        {
            var response = await _availabilityService.GetListingAvailabilityAsync(request.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<GetAvailabilityWindowsResponse>.WithErrors(response.Errors);

            return ServiceResult<GetAvailabilityWindowsResponse>.Success(response.Payload);
        }
    }
}
