using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    public interface IListingService
    {
        Task<ServiceResult<CreateListingResponse>> CreateAsync(long ownerId, string title, string description, decimal pricePerHour,
            double latitude, double longitude, string? address, CancellationToken ct);
        Task<ServiceResult<GetListingByIdResponse>> GetByIdAsync(long id, CancellationToken ct);

        Task<ServiceResult<GetOwnerListingsResponse>> GetByOwnerAsync(long ownerId, CancellationToken ct);

        Task<ServiceResult<UpdateListingResponse>> UpdateAsync(long id, long ownerId, string title, string description, decimal pricePerHour,
            double latitude, double longitude, string? address, string? size, bool isAlwaysAvailable, CancellationToken ct);

        Task<ServiceResult<DeactivateListingResponse>> DeactivateAsync(long id, long ownerId, CancellationToken ct);

        Task<ServiceResult<ActivateListingResponse>> ActivateAsync(long id, long ownerId, CancellationToken ct);

        Task<ServiceResult<SearchListingsResponse>> SearchAsync(double? lat, double? lng, double? radiusKm, decimal? minPrice, decimal? maxPrice, bool? onlyActive, bool? onlyAlwaysAvailable, string? titleContains, string? addressContains, int page, int pageSize, CancellationToken ct);

    }
}
