using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    /// <inheritdoc/>
    public class AvailabilityService : IAvailabilityService
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the availability service.
        /// </summary>
        /// <param name="db">Database context.</param>
        public AvailabilityService(ParkShareDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateAvailabilityWindowResponse>> CreateAsync(long listingId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct)
        {
            var listing = await _db.Listings
                .AsNoTracking()
                .Where(l => l.Id == listingId)
                .Select(l => new { l.Id, l.Title })
                .FirstOrDefaultAsync(ct);

            if (listing is null)
                return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Listing not found.");

            var entity = new AvailabilityWindow
            {
                ListingId = listingId,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                IsBlocked = isBlocked,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _db.AvailabilityWindows.AddAsync(entity, ct);
                await _db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Failed to create availability window.");
            }

            return ServiceResult<CreateAvailabilityWindowResponse>.Success(new CreateAvailabilityWindowResponse
            {
                AvailabilityWindowId = entity.Id,
                ListingId = entity.ListingId,
                ListingTitle = listing.Title,
                StartDateTime = entity.StartDateTime,
                EndDateTime = entity.EndDateTime,
                IsBlocked = entity.IsBlocked,
                Note = entity.Note,
                CreatedAt = entity.CreatedAt
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<UpdateAvailabilityWindowResponse>> UpdateAsync(long availabilityWindowId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct)
        {
            try
            {
                var entity = await _db.AvailabilityWindows.FirstOrDefaultAsync(w => w.Id == availabilityWindowId, ct);
                if (entity is null)
                    return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Availability window not found.");

                entity.StartDateTime = startDateTime;
                entity.EndDateTime = endDateTime;
                entity.IsBlocked = isBlocked;
                entity.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

                await _db.SaveChangesAsync(ct);

                var listingTitle = await _db.Listings
                    .AsNoTracking()
                    .Where(l => l.Id == entity.ListingId)
                    .Select(l => l.Title)
                    .FirstOrDefaultAsync(ct) ?? string.Empty;

                return ServiceResult<UpdateAvailabilityWindowResponse>.Success(new UpdateAvailabilityWindowResponse
                {
                    AvailabilityWindowId = entity.Id,
                    ListingId = entity.ListingId,
                    ListingTitle = listingTitle,
                    StartDateTime = entity.StartDateTime,
                    EndDateTime = entity.EndDateTime,
                    IsBlocked = entity.IsBlocked,
                    Note = entity.Note,
                    CreatedAt = entity.CreatedAt
                });
            }
            catch (DbUpdateException)
            {
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Failed to update availability window.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<DeleteAvailabilityWindowResponse>> DeleteAsync(long availabilityWindowId, CancellationToken ct)
        {
            try
            {
                var entity = await _db.AvailabilityWindows.FirstOrDefaultAsync(w => w.Id == availabilityWindowId, ct);
                if (entity is null)
                    return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Availability window not found.");

                _db.AvailabilityWindows.Remove(entity);
                await _db.SaveChangesAsync(ct);

                return ServiceResult<DeleteAvailabilityWindowResponse>.Success(new DeleteAvailabilityWindowResponse
                {
                    AvailabilityWindowId = availabilityWindowId
                });
            }
            catch (DbUpdateException)
            {
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Failed to delete availability window.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetAvailabilityWindowsResponse>> GetListingAvailabilityAsync(long listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken ct)
        {
            var listingExists = await _db.Listings.AsNoTracking().AnyAsync(l => l.Id == listingId, ct);
            if (!listingExists)
                return ServiceResult<GetAvailabilityWindowsResponse>.WithError("Listing not found.");

            var items = await BuildAvailabilityQuery(startDateTime, endDateTime)
                .Where(w => w.ListingId == listingId)
                .OrderBy(w => w.StartDateTime)
                .ThenBy(w => w.EndDateTime)
                .Select(w => new AvailabilityWindowResponse
                {
                    AvailabilityWindowId = w.Id,
                    ListingId = w.ListingId,
                    ListingTitle = w.Listing.Title,
                    StartDateTime = w.StartDateTime,
                    EndDateTime = w.EndDateTime,
                    IsBlocked = w.IsBlocked,
                    Note = w.Note,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync(ct);

            return ServiceResult<GetAvailabilityWindowsResponse>.Success(new GetAvailabilityWindowsResponse
            {
                ListingId = listingId,
                TotalCount = items.Count,
                Items = items
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetAvailabilityWindowsResponse>> GetOwnerAvailabilityAsync(long ownerId, long? listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken ct)
        {
            var query = BuildAvailabilityQuery(startDateTime, endDateTime)
                .Where(w => w.Listing.OwnerId == ownerId);

            if (listingId.HasValue)
                query = query.Where(w => w.ListingId == listingId.Value);

            var items = await query
                .OrderBy(w => w.ListingId)
                .ThenBy(w => w.StartDateTime)
                .ThenBy(w => w.EndDateTime)
                .Select(w => new AvailabilityWindowResponse
                {
                    AvailabilityWindowId = w.Id,
                    ListingId = w.ListingId,
                    ListingTitle = w.Listing.Title,
                    StartDateTime = w.StartDateTime,
                    EndDateTime = w.EndDateTime,
                    IsBlocked = w.IsBlocked,
                    Note = w.Note,
                    CreatedAt = w.CreatedAt
                })
                .ToListAsync(ct);

            return ServiceResult<GetAvailabilityWindowsResponse>.Success(new GetAvailabilityWindowsResponse
            {
                ListingId = listingId,
                TotalCount = items.Count,
                Items = items
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<AvailabilityWindowCore>> GetWindowCoreAsync(long availabilityWindowId, CancellationToken ct)
        {
            var window = await _db.AvailabilityWindows
                .AsNoTracking()
                .Where(w => w.Id == availabilityWindowId)
                .Select(w => new AvailabilityWindowCore
                {
                    AvailabilityWindowId = w.Id,
                    ListingId = w.ListingId,
                    OwnerId = w.Listing.OwnerId,
                    IsBlocked = w.IsBlocked,
                    StartDateTime = w.StartDateTime,
                    EndDateTime = w.EndDateTime
                })
                .FirstOrDefaultAsync(ct);

            if (window is null)
                return ServiceResult<AvailabilityWindowCore>.WithError("Availability window not found.");

            return ServiceResult<AvailabilityWindowCore>.Success(window);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> HasWindowCoverageAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct)
        {
            var windows = await _db.AvailabilityWindows
                .AsNoTracking()
                .Where(w => w.ListingId == listingId
                    && !w.IsBlocked
                    && w.EndDateTime > startDateTime
                    && w.StartDateTime < endDateTime)
                .OrderBy(w => w.StartDateTime)
                .Select(w => new { w.StartDateTime, w.EndDateTime })
                .ToListAsync(ct);

            var coveredUntil = startDateTime;
            foreach (var window in windows)
            {
                if (window.EndDateTime <= coveredUntil)
                    continue;

                if (window.StartDateTime > coveredUntil)
                    return ServiceResult<bool>.Success(false);

                coveredUntil = window.EndDateTime;
                if (coveredUntil >= endDateTime)
                    return ServiceResult<bool>.Success(true);
            }

            return ServiceResult<bool>.Success(coveredUntil >= endDateTime);
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> HasBlockedOverlapAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct)
        {
            var blocked = await _db.AvailabilityWindows
                .AsNoTracking()
                .AnyAsync(w => w.ListingId == listingId && w.IsBlocked && w.EndDateTime > startDateTime && w.StartDateTime < endDateTime, ct);

            return ServiceResult<bool>.Success(blocked);
        }

        /// <summary>
        /// Builds the base availability query with optional date filtering.
        /// </summary>
        /// <param name="startDateTime">Optional lower bound.</param>
        /// <param name="endDateTime">Optional upper bound.</param>
        /// <returns>The filtered query.</returns>
        private IQueryable<AvailabilityWindow> BuildAvailabilityQuery(DateTime? startDateTime, DateTime? endDateTime)
        {
            var query = _db.AvailabilityWindows
                .AsNoTracking()
                .Include(w => w.Listing)
                .AsQueryable();

            if (startDateTime.HasValue)
                query = query.Where(w => w.EndDateTime > startDateTime.Value);

            if (endDateTime.HasValue)
                query = query.Where(w => w.StartDateTime < endDateTime.Value);

            return query;
        }
    }

}
