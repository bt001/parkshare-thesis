using ParkShare.Domain.Entities;
namespace ParkShare.Application.Interfaces.Repositories;

/// <summary>Persists listing photo metadata within application transactions.</summary>
public interface IListingPhotoRepository
{
    /// <summary>Reads photos in stable upload order.</summary>
    Task<IReadOnlyList<ListingPhoto>> GetByListingAsync(long listingId, CancellationToken ct);
    /// <summary>Reads a photo only within its parent listing.</summary>
    Task<ListingPhoto?> GetAsync(long listingId, long photoId, CancellationToken ct);
    /// <summary>Counts saved photos after the caller acquires the listing lock.</summary>
    Task<int> CountAsync(long listingId, CancellationToken ct);
    /// <summary>Adds photo metadata.</summary>
    Task AddAsync(ListingPhoto photo, CancellationToken ct);
    /// <summary>Deletes photo metadata.</summary>
    Task DeleteAsync(ListingPhoto photo, CancellationToken ct);
}
