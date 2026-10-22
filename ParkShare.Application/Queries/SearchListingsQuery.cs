using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class SearchListingsQuery : IRequest<ServiceResult<SearchListingsResponse>>
    {
        /// <summary>Requested arrival in Romanian local time, paired with departure.</summary>
        public DateTime? StartDateTime { get; init; }
        /// <summary>Requested departure in Romanian local time.</summary>
        public DateTime? EndDateTime { get; init; }
        /// <summary>All selected amenity identifiers must match.</summary>
        public long[]? AmenityIds { get; set; }
        /// <summary>Inclusive unrounded review average threshold.</summary>
        public double? MinRating { get; set; }
        /// <summary>Ordering: newest, priceAsc, priceDesc or ratingDesc.</summary>
        public string? SortBy { get; set; }
        /// <summary>Inclusive southern latitude of the visible search bounds.</summary>
        public double? South { get; init; }
        /// <summary>Inclusive northern latitude of the visible search bounds.</summary>
        public double? North { get; init; }
        /// <summary>Inclusive western longitude; west greater than east crosses the antimeridian.</summary>
        public double? West { get; init; }
        /// <summary>Inclusive eastern longitude of the visible search bounds.</summary>
        public double? East { get; init; }
        public double? Lat { get; init; }
        public double? Lng { get; init; }
        public double? RadiusKm { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public bool? OnlyActive { get; init; }
        public bool? OnlyAlwaysAvailable { get; init; }
        public string? TitleContains { get; init; }
        public string? AddressContains { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
    }

    public class SearchListingsHandler : IRequestHandler<SearchListingsQuery, ServiceResult<SearchListingsResponse>>
    {
        private readonly IListingService _listingService;
        public SearchListingsHandler(IListingService listingService) => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<SearchListingsResponse>> Handle(SearchListingsQuery request, CancellationToken cancellationToken)
        {
            var response = await _listingService.SearchAsync(request.Lat, request.Lng, request.RadiusKm, request.MinPrice, request.MaxPrice, request.OnlyActive, request.OnlyAlwaysAvailable, request.TitleContains, request.AddressContains, request.Page, request.PageSize, cancellationToken, request.StartDateTime, request.EndDateTime, request.AmenityIds, request.MinRating, request.SortBy, request.South, request.North, request.West, request.East);
            if (!response.IsSuccessful)
                return ServiceResult<SearchListingsResponse>.WithErrors(response);

            return ServiceResult<SearchListingsResponse>.Success(response.Payload);
        }
    }
}
