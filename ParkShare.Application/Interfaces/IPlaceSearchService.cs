using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Resolves user-entered places with safe operational errors.</summary>
    public interface IPlaceSearchService
    {
        /// <summary>Searches for geographic candidates without querying listing addresses.</summary>
        Task<ServiceResult<SearchPlacesResponse>> SearchAsync(string text, CancellationToken ct);
        /// <summary>Finds streets suitable for owner address selection.</summary>
        Task<ServiceResult<SearchPlacesResponse>> SearchStreetsAsync(string text, CancellationToken ct);
        /// <summary>Resolves a street and building number with safe operational errors.</summary>
        Task<ServiceResult<SearchPlacesResponse>> ResolveAddressAsync(string street, string houseNumber, string? city, string? postcode, CancellationToken ct);
    }
}
