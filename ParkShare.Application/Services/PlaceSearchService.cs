using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class PlaceSearchService : IPlaceSearchService
    {
        private readonly IPlaceSearchProvider _provider;
        private readonly ILogger<PlaceSearchService> _logger;
        /// <summary>Creates the place search service.</summary>
        public PlaceSearchService(IPlaceSearchProvider provider, ILogger<PlaceSearchService> logger)
        { _provider = provider; _logger = logger; }
        /// <inheritdoc/>
        public async Task<ServiceResult<SearchPlacesResponse>> SearchAsync(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2 || text.Length > 200)
                return ServiceResult<SearchPlacesResponse>.WithError("Enter a place name between 2 and 200 characters.");
            try
            {
                var items = await _provider.SearchAsync(text.Trim(), ct);
                return ServiceResult<SearchPlacesResponse>.Success(new() { Items = items });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                // Provider exceptions may contain credential-bearing URLs; log only the exception type.
                _logger.LogWarning("Place lookup failed ({FailureType}).", ex.GetType().Name);
                return ServiceResult<SearchPlacesResponse>.WithError("Place lookup is unavailable. Please retry, or browse the map and search around its centre.");
            }
        }
    }
}
