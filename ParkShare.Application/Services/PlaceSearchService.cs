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
        public Task<ServiceResult<SearchPlacesResponse>> SearchStreetsAsync(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2 || text.Length > 200)
                return Task.FromResult(ServiceResult<SearchPlacesResponse>.WithError("Enter a street between 2 and 200 characters."));
            return LookupAddressAsync(() => _provider.SearchStreetsAsync(text.Trim(), ct), ct);
        }
        /// <inheritdoc/>
        public Task<ServiceResult<SearchPlacesResponse>> ResolveAddressAsync(string street, string houseNumber, string? city, string? postcode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(street) || street.Length > 200 || string.IsNullOrWhiteSpace(houseNumber)
                || houseNumber.Length > 20 || city?.Length > 200 || postcode?.Length > 20)
                return Task.FromResult(ServiceResult<SearchPlacesResponse>.WithError("Enter a street and building number up to 20 characters, with a valid locality."));
            return LookupAddressAsync(() => _provider.ResolveAddressAsync(street.Trim(), houseNumber.Trim(), city?.Trim(), postcode?.Trim(), ct), ct);
        }
        /// <summary>Handles address provider failures without exposing transport credentials.</summary>
        private async Task<ServiceResult<SearchPlacesResponse>> LookupAddressAsync(Func<Task<IReadOnlyList<PlaceResponse>>> lookup, CancellationToken ct)
        {
            try { return ServiceResult<SearchPlacesResponse>.Success(new() { Items = await lookup() }); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogWarning("Address lookup failed ({FailureType}).", ex.GetType().Name);
                return ServiceResult<SearchPlacesResponse>.WithError("Address lookup is unavailable. Please retry.");
            }
        }
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
