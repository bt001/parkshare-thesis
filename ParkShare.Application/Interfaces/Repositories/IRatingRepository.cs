using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Domain.Entities;
namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>Persists reviews and reads listing aggregates.</summary>
    public interface IRatingRepository
    {
        /// <summary>Upserts a validated review within the caller transaction and listing lock.</summary>
        Task<Rating> SaveAsync(Rating rating, CancellationToken ct);
        /// <summary>Returns a stable review page.</summary>
        Task<PagedResult<Rating>> GetAsync(long listingId, int page, int pageSize, CancellationToken ct);
        /// <summary>Returns average stars across all listing reviews.</summary>
        Task<double?> AverageAsync(long listingId, CancellationToken ct);
    }
}
