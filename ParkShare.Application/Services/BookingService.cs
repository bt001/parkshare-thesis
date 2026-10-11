using Microsoft.Extensions.Logging;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;
using ParkShare.Common.Constants;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IListingRepository _listingRepository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IRepositoryTransactionCoordinator _transactions;
        private readonly BookingTimePolicy _timePolicy;
        private readonly ILogger<BookingService> _logger;

        /// <summary>
        /// Creates the booking application service.
        /// </summary>
        public BookingService(
            IBookingRepository bookingRepository,
            IListingRepository listingRepository,
            IAvailabilityService availabilityService,
            IRepositoryTransactionCoordinator transactions,
            BookingTimePolicy timePolicy,
            ILogger<BookingService> logger,
            INotificationRepository notificationRepository)
        {
            _availabilityService = availabilityService;
            _transactions = transactions;
            _timePolicy = timePolicy;
            _logger = logger;
            _notificationRepository = notificationRepository;
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> HasConfirmedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct)
            => ServiceResult<bool>.Success(await _bookingRepository.HasConfirmedOverlapAsync(listingId, StartDateTime, EndDateTime, ct));

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateBookingResponse>> CreateAsync(CreateBookingCommand cmd, CancellationToken ct)
        {
            try
            {
                await using var transaction = await _transactions.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(cmd.ListingId, ct);
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(cmd.ListingId, ct);
                if (listing is null)
                    return ServiceResult<CreateBookingResponse>.WithError("Listing not found.");
                if (listing.OwnerId == cmd.RenterId)
                    return ServiceResult<CreateBookingResponse>.WithError("Owner cannot book own listing.");
                var error = await ValidatePeriodAsync(listing, cmd.StartDateTime, cmd.EndDateTime, ct);
                if (error is not null)
                    return ServiceResult<CreateBookingResponse>.WithError(error);
                var entity = new Booking
                {
                    ListingId = cmd.ListingId, RenterId = cmd.RenterId,
                    StartDateTime = cmd.StartDateTime, EndDateTime = cmd.EndDateTime,
                    Status = BookingStatus.Pending, CreatedAt = DateTime.UtcNow
                };
                await _bookingRepository.AddAsync(entity, ct);
                await _notificationRepository.AddRangeAsync(new[]
                {
                    CreateNotification(entity, listing.OwnerId, listing.Title, NotificationTypes.BookingRequested, "was requested")
                }, ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<CreateBookingResponse>.Success(new CreateBookingResponse
                {
                    BookingId = entity.Id, ListingId = entity.ListingId, RenterId = entity.RenterId,
                    OwnerId = listing.OwnerId, StartDateTime = entity.StartDateTime,
                    EndDateTime = entity.EndDateTime, Status = entity.Status, CreatedAt = entity.CreatedAt
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking creation failed for listing {ListingId}.", cmd.ListingId);
                return ServiceResult<CreateBookingResponse>.WithError("The booking could not be created. Please try again.");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetBookingByIdResponse>> GetByIdAsync(long bookingId, CancellationToken ct)
        {
            var booking = await _bookingRepository.GetByIdWithListingAsync(bookingId, ct);
            if (booking is null)
                return ServiceResult<GetBookingByIdResponse>.WithError("Booking not found.");

            return ServiceResult<GetBookingByIdResponse>.Success(new GetBookingByIdResponse
            {
                BookingId = booking.Id,
                ListingId = booking.ListingId,
                RenterId = booking.RenterId,
                OwnerId = booking.Listing.OwnerId,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<long>> GetOwnerForBookingAsync(long bookingId, CancellationToken ct)
        {
            var ownerId = await _bookingRepository.GetOwnerIdAsync(bookingId, ct);
            return ownerId.HasValue
                ? ServiceResult<long>.Success(ownerId.Value)
                : ServiceResult<long>.WithError("Booking not found.");
        }

        /// <inheritdoc/>
        public Task<ServiceResult<ApproveBookingResponse>> ApproveAndRejectOverlappingPendingAsync(long bookingId, long ownerId, CancellationToken ct)
            => DecideAsync<ApproveBookingResponse>(bookingId, ownerId, true, ct);

        /// <inheritdoc/>
        public Task<ServiceResult<RejectBookingResponse>> RejectAsync(long bookingId, long ownerId, string? reason, CancellationToken ct)
            => DecideAsync<RejectBookingResponse>(bookingId, ownerId, false, ct);

        /// <summary>Serializes owner decisions and validates fresh state before changing a booking.</summary>
        private async Task<ServiceResult<T>> DecideAsync<T>(long bookingId, long ownerId, bool approve, CancellationToken ct)
            where T : BookingResponse, new()
        {
            try
            {
                var listingId = await _bookingRepository.GetListingIdAsync(bookingId, ct);
                if (!listingId.HasValue) return ServiceResult<T>.WithError("Booking not found.");
                await using var transaction = await _transactions.BeginTransactionAsync(ct);
                await _listingRepository.LockAsync(listingId.Value, ct);
                var booking = await _bookingRepository.GetByIdWithListingAsync(bookingId, ct);
                if (booking is null) return ServiceResult<T>.WithError("Booking not found.");
                if (booking.Listing.OwnerId != ownerId)
                    return ServiceResult<T>.WithError("Not authorized to decide this booking.");
                var target = approve ? BookingStatus.Confirmed : BookingStatus.Rejected;
                if (booking.Status == target)
                    return ServiceResult<T>.Success(MapBooking<T>(booking));
                if (booking.Status != BookingStatus.Pending)
                    return ServiceResult<T>.Conflict("Only pending bookings can be approved or rejected.");
                if (approve)
                {
                    var error = await ValidatePeriodAsync(booking.Listing, booking.StartDateTime, booking.EndDateTime, ct);
                    if (error is not null) return ServiceResult<T>.Conflict(error);
                }
                Booking changed;
                var notifications = new List<Notification>();
                if (approve)
                {
                    var result = await _bookingRepository.ApproveAndRejectOverlappingPendingAsync(bookingId, ct)
                        ?? throw new InvalidOperationException("The locked booking disappeared.");
                    changed = result.Booking;
                    notifications.Add(CreateNotification(changed, changed.RenterId, booking.Listing.Title,
                        NotificationTypes.BookingApproved, "was approved"));
                    notifications.AddRange(result.RejectedBookings.Select(rejected => CreateNotification(
                        rejected, rejected.RenterId, booking.Listing.Title, NotificationTypes.BookingOverlapRejected,
                        "was rejected because an overlapping booking was approved")));
                }
                else
                {
                    changed = await _bookingRepository.RejectAsync(bookingId, ct)
                        ?? throw new InvalidOperationException("The locked booking disappeared.");
                    notifications.Add(CreateNotification(changed, changed.RenterId, booking.Listing.Title,
                        NotificationTypes.BookingRejected, "was rejected"));
                }
                await _notificationRepository.AddRangeAsync(notifications, ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<T>.Success(MapBooking<T>(changed!));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Booking decision failed for booking {BookingId}.", bookingId);
                return ServiceResult<T>.WithError("The booking decision could not be saved. Please try again.");
            }
        }

        /// <summary>Creates a server-owned event with useful booking context.</summary>
        private static Notification CreateNotification(Booking booking, long recipient, string title, string type, string action)
            => new()
            {
                UserId = recipient, BookingId = booking.Id, Type = type,
                Message = $"Booking #{booking.Id} for {title} {action}.", CreatedAt = DateTime.UtcNow
            };

        /// <summary>Checks booking time and availability while the caller holds the listing lock.</summary>
        private async Task<string?> ValidatePeriodAsync(Listing listing, DateTime start, DateTime end, CancellationToken ct)
        {
            var timeError = _timePolicy.Validate(start, end);
            if (timeError is not null) return timeError;
            if (!listing.IsActive) return "Listing is not active.";
            if (!listing.IsAlwaysAvailable)
            {
                var coverage = await _availabilityService.HasWindowCoverageAsync(listing.Id, start, end, ct);
                if (!coverage.IsSuccessful) return coverage.ErrorsSummary;
                if (!coverage.Payload) return "Requested time is outside available windows.";
            }
            var blocked = await _availabilityService.HasBlockedOverlapAsync(listing.Id, start, end, ct);
            if (!blocked.IsSuccessful) return blocked.ErrorsSummary;
            if (blocked.Payload) return "Requested time overlaps a blocked window.";
            if (await _bookingRepository.HasConfirmedOverlapAsync(listing.Id, start, end, ct))
                return "Requested time overlaps a confirmed booking.";
            return null;
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetOwnerBookingsResponse>> GetOwnerByStatusesAsync(long ownerId, long? listingId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken ct)
        {
            var results = await _bookingRepository.GetOwnerByStatusesAsync(ownerId, listingId, statuses, page, pageSize, ct);

            return ServiceResult<GetOwnerBookingsResponse>.Success(new GetOwnerBookingsResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = results.TotalCount,
                Items = results.Items.Select(booking => new OwnerBookingItem
                {
                    BookingId = booking.Id,
                    ListingId = booking.ListingId,
                    ListingTitle = booking.Listing.Title,
                    RenterId = booking.RenterId,
                    StartUtc = booking.StartDateTime,
                    EndUtc = booking.EndDateTime,
                    Status = booking.Status,
                    CreatedAt = booking.CreatedAt
                }).ToList()
            });
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<GetRenterBookingsResponse>> GetRenterByStatusesAsync(long renterId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken cancellationToken)
        {
            try
            {
                var results = await _bookingRepository.GetRenterByStatusesAsync(renterId, statuses, page, pageSize, cancellationToken);
                return ServiceResult<GetRenterBookingsResponse>.Success(new GetRenterBookingsResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = results.TotalCount,
                    Items = results.Items.Select(booking => new RenterBookingItem
                    {
                        BookingId = booking.Id,
                        ListingId = booking.ListingId,
                        ListingTitle = booking.Listing.Title,
                        ListingAddress = booking.Listing.Address ?? string.Empty,
                        StartDateTime = booking.StartDateTime,
                        EndDateTime = booking.EndDateTime,
                        Status = booking.Status,
                        CreatedAt = booking.CreatedAt
                    }).ToList()
                });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return ServiceResult<GetRenterBookingsResponse>.WithError("Booking history could not be loaded. Please try again.");
            }
        }

        /// <summary>
        /// Maps a booking entity into the requested booking response type.
        /// </summary>
        private static T MapBooking<T>(Booking booking)
            where T : BookingResponse, new()
            => new()
            {
                BookingId = booking.Id,
                ListingId = booking.ListingId,
                RenterId = booking.RenterId,
                OwnerId = booking.Listing.OwnerId,
                StartDateTime = booking.StartDateTime,
                EndDateTime = booking.EndDateTime,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            };
    }
}
