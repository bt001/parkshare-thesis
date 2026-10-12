using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Provides the public amenity catalog.</summary>
    public interface IAmenityService
    {
        /// <summary>Loads all catalog entries.</summary>
        Task<ServiceResult<GetAmenitiesResponse>> GetAllAsync(CancellationToken ct);
    }
}
