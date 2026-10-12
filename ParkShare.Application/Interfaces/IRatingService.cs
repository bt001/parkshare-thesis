using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Coordinates completed-booking reviews and public listing review reads.</summary>
    public interface IRatingService
    {
        /// <summary>Creates or updates the authenticated renter's review under the listing lock.</summary>
        Task<ServiceResult<RatingResponse>> SaveAsync(long bookingId, long renterId, int stars, string? comment, CancellationToken ct);
        /// <summary>Returns a bounded listing review page and aggregate.</summary>
        Task<ServiceResult<GetListingRatingsResponse>> GetAsync(long listingId, int page, int pageSize, CancellationToken ct);
    }
}
