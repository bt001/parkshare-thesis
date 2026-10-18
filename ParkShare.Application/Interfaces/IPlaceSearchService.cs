using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Resolves user-entered places with safe operational errors.</summary>
    public interface IPlaceSearchService
    {
        /// <summary>Searches for geographic candidates without querying listing addresses.</summary>
        Task<ServiceResult<SearchPlacesResponse>> SearchAsync(string text, CancellationToken ct);
    }
}
