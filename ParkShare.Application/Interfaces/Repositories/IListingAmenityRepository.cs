using ParkShare.Domain.Entities;
namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>Persists amenity selections inside the caller's listing transaction.</summary>
    public interface IListingAmenityRepository
    {
        /// <summary>Returns selected amenities ordered by name and identifier.</summary>
        Task<IReadOnlyList<Amenity>> GetByListingAsync(long listingId, CancellationToken ct);
        /// <summary>Replaces selections and saves changes; requires a transaction and listing lock.</summary>
        Task ReplaceAsync(long listingId, IReadOnlyCollection<long> amenityIds, CancellationToken ct);
    }
}
