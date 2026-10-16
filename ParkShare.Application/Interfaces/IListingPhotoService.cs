using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces;

/// <summary>Coordinates listing photo ownership, bounds and storage.</summary>
public interface IListingPhotoService
{
    /// <summary>Reads public metadata for an existing listing.</summary>
    Task<ServiceResult<ListingPhotosResponse>> GetAsync(long listingId, CancellationToken ct);
    /// <summary>Reads content only when matching photo metadata exists.</summary>
    Task<ServiceResult<ListingPhotoContentResponse>> GetContentAsync(long listingId, long photoId, CancellationToken ct);
    /// <summary>Uploads a bounded supported image for the current owner.</summary>
    Task<ServiceResult<ListingPhotoResponse>> UploadAsync(long listingId, long ownerId, Stream content, string contentType, CancellationToken ct);
    /// <summary>Removes an owner's photo before cleaning up its stored file.</summary>
    Task<ServiceResult<DeleteListingPhotoResponse>> DeleteAsync(long listingId, long photoId, long ownerId, CancellationToken ct);
}
