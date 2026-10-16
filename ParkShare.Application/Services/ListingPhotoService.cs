using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Constants;
using ParkShare.Domain.Entities;
namespace ParkShare.Application.Services;

/// <inheritdoc/>
public class ListingPhotoService : IListingPhotoService
{
    private readonly IListingRepository _listings;
    private readonly IListingPhotoRepository _photos;
    private readonly IListingPhotoStorage _storage;
    private readonly IRepositoryTransactionCoordinator _transactions;
    private readonly ILogger<ListingPhotoService> _logger;
    /// <summary>Creates the photo workflow with application-owned transactions.</summary>
    public ListingPhotoService(IListingRepository listings, IListingPhotoRepository photos, IListingPhotoStorage storage,
        IRepositoryTransactionCoordinator transactions, ILogger<ListingPhotoService> logger)
        => (_listings, _photos, _storage, _transactions, _logger) = (listings, photos, storage, transactions, logger);

    /// <summary>Builds public metadata without exposing filesystem keys.</summary>
    private static ListingPhotoResponse Metadata(ListingPhoto photo) => new()
    {
        Id = photo.Id, UploadedAt = DateTime.SpecifyKind(photo.UploadedAt, DateTimeKind.Utc),
        Url = $"api/listing/{photo.ListingId}/photos/{photo.Id}/content"
    };

    /// <summary>Identifies supported signatures and required image header/trailer markers.</summary>
    private static string? ImageType(byte[] bytes)
    {
        if (bytes.Length >= 33 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && bytes.AsSpan(12, 4).SequenceEqual("IHDR"u8)
            && bytes.AsSpan(bytes.Length - 8, 4).SequenceEqual("IEND"u8)) return "image/png";
        if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255
            && bytes[^2] == 255 && bytes[^1] == 217) return "image/jpeg";
        return null;
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ListingPhotosResponse>> GetAsync(long listingId, CancellationToken ct)
    {
        try
        {
            if (await _listings.GetByIdAsNoTrackingAsync(listingId, ct) is null)
                return ServiceResult<ListingPhotosResponse>.WithError("Listing not found.");
            var photos = await _photos.GetByListingAsync(listingId, ct);
            return ServiceResult<ListingPhotosResponse>.Success(new() { Items = photos.Select(Metadata).ToList() });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo retrieval failed for listing {ListingId}.", listingId);
            return ServiceResult<ListingPhotosResponse>.WithError("Photos could not be loaded. Please retry.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ListingPhotoContentResponse>> GetContentAsync(long listingId, long photoId, CancellationToken ct)
    {
        try
        {
            var photo = await _photos.GetAsync(listingId, photoId, ct);
            var content = photo is null ? null : await _storage.ReadAsync(photo.Url, ct);
            if (content is null) return ServiceResult<ListingPhotoContentResponse>.WithError("Photo not found.");
            var type = ImageType(content);
            if (type is null) throw new InvalidDataException("Stored photo signature is invalid.");
            return ServiceResult<ListingPhotoContentResponse>.Success(new() { Content = content, ContentType = type });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo content retrieval failed for photo {PhotoId}.", photoId);
            return ServiceResult<ListingPhotoContentResponse>.WithError("Photo could not be loaded. Please retry.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<ListingPhotoResponse>> UploadAsync(long listingId, long ownerId, Stream content, string contentType, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            if (contentType is not ("image/jpeg" or "image/png"))
                return ServiceResult<ListingPhotoResponse>.WithError("Select a JPEG or PNG image up to 5 MiB.");
            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int count;
            while ((count = await content.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, ListingPhotoLimits.MaxBytes + 1 - (int)buffer.Length)), ct)) > 0)
            {
                buffer.Write(chunk, 0, count);
                if (buffer.Length > ListingPhotoLimits.MaxBytes)
                    return ServiceResult<ListingPhotoResponse>.WithError("Photo must not exceed 5 MiB.");
            }
            var bytes = buffer.ToArray();
            if (ImageType(bytes) != contentType)
                return ServiceResult<ListingPhotoResponse>.WithError("The file does not match a supported JPEG or PNG image.");

            await using var transaction = await _transactions.BeginTransactionAsync(ct);
            string? key = null;
            var commitAttempted = false;
            try
            {
                await _listings.LockAsync(listingId, ct);
                var listing = await _listings.GetByIdAsNoTrackingAsync(listingId, ct);
                if (listing is null) return ServiceResult<ListingPhotoResponse>.WithError("Listing not found.");
                if (listing.OwnerId != ownerId) return ServiceResult<ListingPhotoResponse>.WithError("Not authorized to change these photos.");
                if (await _photos.CountAsync(listingId, ct) >= ListingPhotoLimits.MaxPhotos)
                    return ServiceResult<ListingPhotoResponse>.Conflict("A listing can have at most 8 photos.");
                key = Guid.NewGuid().ToString("N") + (contentType == "image/png" ? ".png" : ".jpg");
                await _storage.WriteAsync(key, bytes, ct);
                var photo = new ListingPhoto { ListingId = listingId, Url = key, UploadedAt = DateTime.UtcNow };
                await _photos.AddAsync(photo, ct);
                commitAttempted = true;
                await transaction.CommitAsync(ct);
                return ServiceResult<ListingPhotoResponse>.Success(Metadata(photo));
            }
            catch
            {
                // A lost commit acknowledgement may still represent a durable row; retain its file.
                if (!commitAttempted)
                {
                    try
                    {
                        await transaction.RollbackAsync(CancellationToken.None);
                        if (key is not null) await CleanupAsync(key);
                    }
                    catch (Exception ex) { _logger.LogError(ex, "Photo rollback could not be confirmed for listing {ListingId}.", listingId); }
                }
                throw;
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo upload failed for listing {ListingId}.", listingId);
            return ServiceResult<ListingPhotoResponse>.WithError("Photo upload could not be confirmed. Refresh photos before retrying.");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<DeleteListingPhotoResponse>> DeleteAsync(long listingId, long photoId, long ownerId, CancellationToken ct)
    {
        try
        {
            string key;
            await using (var transaction = await _transactions.BeginTransactionAsync(ct))
            {
                await _listings.LockAsync(listingId, ct);
                var listing = await _listings.GetByIdAsNoTrackingAsync(listingId, ct);
                if (listing is null) return ServiceResult<DeleteListingPhotoResponse>.WithError("Listing not found.");
                if (listing.OwnerId != ownerId) return ServiceResult<DeleteListingPhotoResponse>.WithError("Not authorized to change these photos.");
                var photo = await _photos.GetAsync(listingId, photoId, ct);
                if (photo is null) return ServiceResult<DeleteListingPhotoResponse>.WithError("Photo not found.");
                key = photo.Url;
                await _photos.DeleteAsync(photo, ct);
                await transaction.CommitAsync(ct);
            }
            await CleanupAsync(key);
            ct.ThrowIfCancellationRequested();
            return ServiceResult<DeleteListingPhotoResponse>.Success(new() { Id = photoId });
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Photo removal failed for photo {PhotoId}.", photoId);
            return ServiceResult<DeleteListingPhotoResponse>.WithError("Photo removal could not be confirmed. Refresh photos before retrying.");
        }
    }

    /// <summary>Performs best-effort cleanup without allowing cleanup failure to undo committed metadata.</summary>
    private async Task CleanupAsync(string key)
    {
        try { await _storage.DeleteAsync(key, CancellationToken.None); }
        catch (Exception ex) { _logger.LogWarning(ex, "Photo file cleanup failed for key {StorageKey}.", key); }
    }
}
