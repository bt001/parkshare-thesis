using Microsoft.Extensions.Logging;
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
        private readonly IBookingRepository _bookingRepository;
        private readonly IRepositoryTransactionCoordinator _transactions;
        private readonly ILogger<AvailabilityService> _logger;
        private readonly IListingRepository _listingRepository;
        private readonly IAvailabilityWindowRepository _availabilityWindowRepository;

        /// <summary>
        /// Creates the availability application service.
        /// </summary>
        public AvailabilityService(
            IListingRepository listingRepository,
            IAvailabilityWindowRepository availabilityWindowRepository,
            IBookingRepository bookingRepository,
            IRepositoryTransactionCoordinator transactions,
            ILogger<AvailabilityService> logger)
        {
            _bookingRepository = bookingRepository;
            _transactions = transactions;
            _logger = logger;
            _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
            _availabilityWindowRepository = availabilityWindowRepository ?? throw new ArgumentNullException(nameof(availabilityWindowRepository));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateAvailabilityWindowResponse>> CreateAsync(long listingId, long ownerId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct)
        {
            try
            {
                await using var transaction = await _transactions.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(listingId, ct);
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(listingId, ct);
                if (listing is null) return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Listing not found.");
                if (listing.OwnerId != ownerId)
                    return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Not authorized to change this availability window.");
                if (startDateTime >= endDateTime)
                    return ServiceResult<CreateAvailabilityWindowResponse>.WithError("End time must be after start time.");
                if (!isBlocked && listing.IsAlwaysAvailable)
                    return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Always-available listings cannot add open availability windows.");
                if (isBlocked && await _bookingRepository.HasConfirmedOverlapAsync(listingId, startDateTime, endDateTime, ct))
                    return ServiceResult<CreateAvailabilityWindowResponse>.Conflict("Blocked windows cannot overlap confirmed bookings.");
                var entity = new AvailabilityWindow { ListingId = listingId, CreatedAt = DateTime.UtcNow };
                entity.StartDateTime = startDateTime;
                entity.EndDateTime = endDateTime;
                entity.IsBlocked = isBlocked;
                entity.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                await _availabilityWindowRepository.AddAsync(entity, ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<CreateAvailabilityWindowResponse>.Success(new CreateAvailabilityWindowResponse
                {
                    AvailabilityWindowId = entity.Id, ListingId = listingId, ListingTitle = listing.Title,
                    StartDateTime = entity.StartDateTime, EndDateTime = entity.EndDateTime,
                    IsBlocked = entity.IsBlocked, Note = entity.Note, CreatedAt = entity.CreatedAt
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Availability create failed.");
                return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Availability could not be saved. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<UpdateAvailabilityWindowResponse>> UpdateAsync(long availabilityWindowId, long ownerId, DateTime startDateTime, DateTime endDateTime, bool isBlocked, string? note, CancellationToken ct)
        {
            try
            {
                var targetId = await _availabilityWindowRepository.GetListingIdAsync(availabilityWindowId, ct);
                if (!targetId.HasValue) return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Availability window not found.");
                var listingId = targetId.Value;
                await using var transaction = await _transactions.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(listingId, ct);
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(listingId, ct);
                if (listing is null) return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Listing not found.");
                if (listing.OwnerId != ownerId)
                    return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Not authorized to change this availability window.");
                var entity = await _availabilityWindowRepository.GetByIdAsync(availabilityWindowId, ct);
                if (entity is null) return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Availability window not found.");
                if (startDateTime >= endDateTime)
                    return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("End time must be after start time.");
                if (!isBlocked && listing.IsAlwaysAvailable)
                    return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Always-available listings cannot add open availability windows.");
                if (isBlocked && await _bookingRepository.HasConfirmedOverlapAsync(listingId, startDateTime, endDateTime, ct))
                    return ServiceResult<UpdateAvailabilityWindowResponse>.Conflict("Blocked windows cannot overlap confirmed bookings.");
                entity.StartDateTime = startDateTime;
                entity.EndDateTime = endDateTime;
                entity.IsBlocked = isBlocked;
                entity.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
                await _availabilityWindowRepository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<UpdateAvailabilityWindowResponse>.Success(new UpdateAvailabilityWindowResponse
                {
                    AvailabilityWindowId = entity.Id, ListingId = listingId, ListingTitle = listing.Title,
                    StartDateTime = entity.StartDateTime, EndDateTime = entity.EndDateTime,
                    IsBlocked = entity.IsBlocked, Note = entity.Note, CreatedAt = entity.CreatedAt
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Availability update failed.");
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Availability could not be saved. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<DeleteAvailabilityWindowResponse>> DeleteAsync(long availabilityWindowId, long ownerId, CancellationToken ct)
        {
            try
            {
                var targetId = await _availabilityWindowRepository.GetListingIdAsync(availabilityWindowId, ct);
                if (!targetId.HasValue) return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Availability window not found.");
                var listingId = targetId.Value;
                await using var transaction = await _transactions.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(listingId, ct);
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(listingId, ct);
                if (listing is null) return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Listing not found.");
                if (listing.OwnerId != ownerId)
                    return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Not authorized to change this availability window.");
                var entity = await _availabilityWindowRepository.GetByIdAsync(availabilityWindowId, ct);
                if (entity is null) return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Availability window not found.");
                await _availabilityWindowRepository.DeleteAsync(entity, ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<DeleteAvailabilityWindowResponse>.Success(new DeleteAvailabilityWindowResponse { AvailabilityWindowId = availabilityWindowId });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Availability delete failed.");
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Availability could not be saved. Please try again.");
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
