using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    public interface IListingReadService
    {
        Task<ServiceResult<ListingCore>> GetCoreAsync(long listingId, CancellationToken ct);
    }
}
