using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class SearchListingsQuery : IRequest<ServiceResult<SearchListingsResponse>>
    {
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
            var response = await _listingService.SearchAsync(request.Lat, request.Lng, request.RadiusKm, request.MinPrice, request.MaxPrice, request.OnlyActive, request.OnlyAlwaysAvailable, request.TitleContains, request.AddressContains, request.Page, request.PageSize, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<SearchListingsResponse>.WithErrors(response.Errors);

            return ServiceResult<SearchListingsResponse>.Success(response.Payload);
        }
    }
}
