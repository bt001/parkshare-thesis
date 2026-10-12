using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>GetListingRatings operation input.</summary>
    public class GetListingRatingsQuery : IRequest<ServiceResult<GetListingRatingsResponse>>
    {
        /// <summary>Listing identity.</summary>
        public long ListingId { get; init; }
        /// <summary>One-based page.</summary>
        public int Page { get; init; } = 1;
        /// <summary>Page size, at most one hundred.</summary>
        public int PageSize { get; init; } = 20;
    }
    /// <summary>Delegates validation and persistence to the application service.</summary>
    public class GetListingRatingsHandler : IRequestHandler<GetListingRatingsQuery, ServiceResult<GetListingRatingsResponse>>
    {
        private readonly IRatingService _service;
        /// <summary>Creates the handler.</summary>
        public GetListingRatingsHandler(IRatingService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<GetListingRatingsResponse>> Handle(GetListingRatingsQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.GetAsync(request.ListingId, request.Page, request.PageSize, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<GetListingRatingsResponse>.WithErrors(result);
            return ServiceResult<GetListingRatingsResponse>.Success(result.Payload);
        }
    }
}
