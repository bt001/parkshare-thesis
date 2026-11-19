using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>Requests streets for owner location selection.</summary>
    public class SearchStreetsQuery : IRequest<ServiceResult<SearchPlacesResponse>>
    {
        /// <summary>Street search text.</summary>
        public string Text { get; set; } = string.Empty;
    }
    /// <summary>Requests coordinates for a structured address.</summary>
    public class ResolveAddressQuery : IRequest<ServiceResult<SearchPlacesResponse>>
    {
        /// <summary>Selected street name.</summary>
        public string Street { get; set; } = string.Empty;
        /// <summary>Building number.</summary>
        public string HouseNumber { get; set; } = string.Empty;
        /// <summary>Selected locality.</summary>
        public string? City { get; set; }
        /// <summary>Selected postal code.</summary>
        public string? Postcode { get; set; }
    }
    /// <summary>Orchestrates street suggestions through the application service.</summary>
    public class SearchStreetsHandler : IRequestHandler<SearchStreetsQuery, ServiceResult<SearchPlacesResponse>>
    {
        private readonly IPlaceSearchService _service;
        /// <summary>Creates the handler.</summary>
        public SearchStreetsHandler(IPlaceSearchService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<SearchPlacesResponse>> Handle(SearchStreetsQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.SearchStreetsAsync(request.Text, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<SearchPlacesResponse>.WithErrors(result);
            return ServiceResult<SearchPlacesResponse>.Success(result.Payload);
        }
    }
    /// <summary>Orchestrates structured address resolution through the application service.</summary>
    public class ResolveAddressHandler : IRequestHandler<ResolveAddressQuery, ServiceResult<SearchPlacesResponse>>
    {
        private readonly IPlaceSearchService _service;
        /// <summary>Creates the handler.</summary>
        public ResolveAddressHandler(IPlaceSearchService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<SearchPlacesResponse>> Handle(ResolveAddressQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.ResolveAddressAsync(request.Street, request.HouseNumber, request.City, request.Postcode, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<SearchPlacesResponse>.WithErrors(result);
            return ServiceResult<SearchPlacesResponse>.Success(result.Payload);
        }
    }
}
