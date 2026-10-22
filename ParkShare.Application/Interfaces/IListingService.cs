using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;

namespace ParkShare.Application.Interfaces
{
    public interface IListingService
    {
        /// <summary>Creates a listing and optional amenity selections atomically.</summary>
        Task<ServiceResult<CreateListingResponse>> CreateAsync(long ownerId, string title, string description, decimal pricePerHour,
            double latitude, double longitude, string? address, string? size, bool isAlwaysAvailable, CancellationToken ct, IReadOnlyCollection<long>? amenityIds = null);
        /// <summary>Loads listing details including selected amenities.</summary>
        Task<ServiceResult<GetListingByIdResponse>> GetByIdAsync(long id, CancellationToken ct);

        Task<ServiceResult<GetOwnerListingsResponse>> GetByOwnerAsync(long ownerId, CancellationToken ct);

        /// <summary>Updates an owned listing atomically; null amenities preserve selections and empty clears them.</summary>
        Task<ServiceResult<UpdateListingResponse>> UpdateAsync(long id, long ownerId, string title, string description, decimal pricePerHour,
            double latitude, double longitude, string? address, string? size, bool isAlwaysAvailable, CancellationToken ct, IReadOnlyCollection<long>? amenityIds = null);

        Task<ServiceResult<DeactivateListingResponse>> DeactivateAsync(long id, long ownerId, CancellationToken ct);

        Task<ServiceResult<ActivateListingResponse>> ActivateAsync(long id, long ownerId, CancellationToken ct);

        /// <summary>Searches parking by distance, filters and an optional complete Romanian booking period.</summary>
        Task<ServiceResult<SearchListingsResponse>> SearchAsync(double? lat, double? lng, double? radiusKm, decimal? minPrice, decimal? maxPrice, bool? onlyActive, bool? onlyAlwaysAvailable, string? titleContains, string? addressContains, int page, int pageSize, CancellationToken ct, DateTime? startDateTime = null, DateTime? endDateTime = null, IReadOnlyCollection<long>? amenityIds = null, double? minRating = null, string? sortBy = null, double? south = null, double? north = null, double? west = null, double? east = null);

    }
}
