using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>Requests geographic candidates for a place name.</summary>
    public class SearchPlacesQuery : IRequest<ServiceResult<SearchPlacesResponse>>
    {
        /// <summary>Submitted city, address or landmark name.</summary>
        public string Text { get; set; } = string.Empty;
    }
    /// <summary>Orchestrates place lookup through the application service.</summary>
    public class SearchPlacesHandler : IRequestHandler<SearchPlacesQuery, ServiceResult<SearchPlacesResponse>>
    {
        private readonly IPlaceSearchService _service;
        /// <summary>Creates the lookup handler.</summary>
        public SearchPlacesHandler(IPlaceSearchService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<SearchPlacesResponse>> Handle(SearchPlacesQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.SearchAsync(request.Text, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<SearchPlacesResponse>.WithErrors(result);
            return ServiceResult<SearchPlacesResponse>.Success(result.Payload);
        }
    }
}
