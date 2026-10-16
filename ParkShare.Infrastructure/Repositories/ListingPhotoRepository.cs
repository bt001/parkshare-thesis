using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
namespace ParkShare.Infrastructure.Repositories;

/// <inheritdoc/>
public class ListingPhotoRepository : IListingPhotoRepository
{
    private readonly ParkShareDbContext _db;
    /// <summary>Creates the photo repository.</summary>
    public ListingPhotoRepository(ParkShareDbContext db) => _db = db;
    /// <inheritdoc/>
    public async Task<IReadOnlyList<ListingPhoto>> GetByListingAsync(long listingId, CancellationToken ct)
        => await _db.ListingPhotos.AsNoTracking().Where(p => p.ListingId == listingId).OrderBy(p => p.UploadedAt).ThenBy(p => p.Id).ToListAsync(ct);
    /// <inheritdoc/>
    public Task<ListingPhoto?> GetAsync(long listingId, long photoId, CancellationToken ct)
        => _db.ListingPhotos.AsNoTracking().FirstOrDefaultAsync(p => p.ListingId == listingId && p.Id == photoId, ct);
    /// <inheritdoc/>
    public Task<int> CountAsync(long listingId, CancellationToken ct) => _db.ListingPhotos.CountAsync(p => p.ListingId == listingId, ct);
    /// <inheritdoc/>
    public async Task AddAsync(ListingPhoto photo, CancellationToken ct)
    {
        _db.ListingPhotos.Add(photo);
        await _db.SaveChangesAsync(ct);
    }
    /// <inheritdoc/>
    public async Task DeleteAsync(ListingPhoto photo, CancellationToken ct)
    {
        await _db.ListingPhotos.Where(p => p.Id == photo.Id && p.ListingId == photo.ListingId).ExecuteDeleteAsync(ct);
    }
}
