using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IListingRepository _listingRepository;
        private readonly IAvailabilityWindowRepository _availabilityWindowRepository;

        /// <summary>
        /// Creates the availability application service.
        /// </summary>
        public AvailabilityService(IListingRepository listingRepository, IAvailabilityWindowRepository availabilityWindowRepository)
        {
            _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
            _availabilityWindowRepository = availabilityWindowRepository ?? throw new ArgumentNullException(nameof(availabilityWindowRepository));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateAvailabilityWindowResponse>> CreateAsync(long listingId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct)
        {
            var listing = await _listingRepository.GetByIdAsNoTrackingAsync(listingId, ct);
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
                await _availabilityWindowRepository.AddAsync(entity, ct);
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
            catch (Exception ex)
            {
                return ServiceResult<CreateAvailabilityWindowResponse>.WithError(ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<UpdateAvailabilityWindowResponse>> UpdateAsync(long availabilityWindowId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct)
        {
            var entity = await _availabilityWindowRepository.GetByIdAsync(availabilityWindowId, ct);
            if (entity is null)
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Availability window not found.");

            entity.StartDateTime = startDateTime;
            entity.EndDateTime = endDateTime;
            entity.IsBlocked = isBlocked;
            entity.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

            try
            {
                await _availabilityWindowRepository.SaveChangesAsync(ct);
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(entity.ListingId, ct);
                return ServiceResult<UpdateAvailabilityWindowResponse>.Success(new UpdateAvailabilityWindowResponse
                {
                    AvailabilityWindowId = entity.Id,
                    ListingId = entity.ListingId,
                    ListingTitle = listing?.Title ?? string.Empty,
                    StartDateTime = entity.StartDateTime,
                    EndDateTime = entity.EndDateTime,
                    IsBlocked = entity.IsBlocked,
                    Note = entity.Note,
                    CreatedAt = entity.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithError(ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<DeleteAvailabilityWindowResponse>> DeleteAsync(long availabilityWindowId, CancellationToken ct)
        {
            var entity = await _availabilityWindowRepository.GetByIdAsync(availabilityWindowId, ct);
            if (entity is null)
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Availability window not found.");

            try
            {
                await _availabilityWindowRepository.DeleteAsync(entity, ct);
                return ServiceResult<DeleteAvailabilityWindowResponse>.Success(new DeleteAvailabilityWindowResponse
                {
                    AvailabilityWindowId = availabilityWindowId
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithError(ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetAvailabilityWindowsResponse>> GetListingAvailabilityAsync(long listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken ct)
        {
            var listing = await _listingRepository.GetByIdAsNoTrackingAsync(listingId, ct);
            if (listing is null)
                return ServiceResult<GetAvailabilityWindowsResponse>.WithError("Listing not found.");

            var windows = await _availabilityWindowRepository.GetByListingAsync(listingId, startDateTime, endDateTime, ct);
            return ServiceResult<GetAvailabilityWindowsResponse>.Success(new GetAvailabilityWindowsResponse
            {
                ListingId = listingId,
                TotalCount = windows.Count,
                Items = windows.Select(MapWindow).ToList()
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetAvailabilityWindowsResponse>> GetOwnerAvailabilityAsync(long ownerId, long? listingId, DateTime? startDateTime, DateTime? endDateTime, CancellationToken ct)
        {
            var windows = await _availabilityWindowRepository.GetByOwnerAsync(ownerId, listingId, startDateTime, endDateTime, ct);
            return ServiceResult<GetAvailabilityWindowsResponse>.Success(new GetAvailabilityWindowsResponse
            {
                ListingId = listingId,
                TotalCount = windows.Count,
                Items = windows.Select(MapWindow).ToList()
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<AvailabilityWindowCore>> GetWindowCoreAsync(long availabilityWindowId, CancellationToken ct)
        {
            var window = await _availabilityWindowRepository.GetByIdWithListingAsync(availabilityWindowId, ct);
            if (window is null)
                return ServiceResult<AvailabilityWindowCore>.WithError("Availability window not found.");

            return ServiceResult<AvailabilityWindowCore>.Success(new AvailabilityWindowCore
            {
                AvailabilityWindowId = window.Id,
                ListingId = window.ListingId,
                OwnerId = window.Listing.OwnerId,
                IsBlocked = window.IsBlocked,
                StartDateTime = window.StartDateTime,
                EndDateTime = window.EndDateTime
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> HasWindowCoverageAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken ct)
        {
            var windows = await _availabilityWindowRepository.GetOpenOverlappingWindowsAsync(listingId, startDateTime, endDateTime, ct);

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
            => ServiceResult<bool>.Success(await _availabilityWindowRepository.HasBlockedOverlapAsync(listingId, startDateTime, endDateTime, ct));

        /// <summary>
        /// Maps a repository availability window to the response shape.
        /// </summary>
        private static AvailabilityWindowResponse MapWindow(AvailabilityWindow window)
            => new()
            {
                AvailabilityWindowId = window.Id,
                ListingId = window.ListingId,
                ListingTitle = window.Listing.Title,
                StartDateTime = window.StartDateTime,
                EndDateTime = window.EndDateTime,
                IsBlocked = window.IsBlocked,
                Note = window.Note,
                CreatedAt = window.CreatedAt
            };
    }
}
