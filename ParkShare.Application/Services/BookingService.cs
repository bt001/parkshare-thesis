using ParkShare.Application.Commands;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IListingRepository _listingRepository;

        /// <summary>
        /// Creates the booking application service.
        /// </summary>
        public BookingService(IBookingRepository bookingRepository, IListingRepository listingRepository)
        {
            _bookingRepository = bookingRepository ?? throw new ArgumentNullException(nameof(bookingRepository));
            _listingRepository = listingRepository ?? throw new ArgumentNullException(nameof(listingRepository));
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> HasConfirmedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct)
            => ServiceResult<bool>.Success(await _bookingRepository.HasConfirmedOverlapAsync(listingId, StartDateTime, EndDateTime, ct));

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateBookingResponse>> CreateAsync(CreateBookingCommand cmd, CancellationToken ct)
        {
            var entity = new Booking
            {
                ListingId = cmd.ListingId,
                RenterId = cmd.RenterId,
                StartDateTime = cmd.StartDateTime,
                EndDateTime = cmd.EndDateTime,
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _bookingRepository.AddAsync(entity, ct);
                var listing = await _listingRepository.GetByIdAsNoTrackingAsync(entity.ListingId, ct);

                return ServiceResult<CreateBookingResponse>.Success(new CreateBookingResponse
                {
                    BookingId = entity.Id,
                    ListingId = entity.ListingId,
                    RenterId = entity.RenterId,
                    OwnerId = listing?.OwnerId ?? 0,
                    StartDateTime = entity.StartDateTime,
                    EndDateTime = entity.EndDateTime,
                    Status = entity.Status,
                    CreatedAt = entity.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return ServiceResult<CreateBookingResponse>.WithError(ex.Message);
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
        public async Task<ServiceResult<ApproveBookingResponse>> ApproveAndRejectOverlappingPendingAsync(long bookingId, CancellationToken ct)
        {
            try
            {
                var booking = await _bookingRepository.ApproveAndRejectOverlappingPendingAsync(bookingId, ct);
                if (booking is null)
                    return ServiceResult<ApproveBookingResponse>.WithError("Booking not found.");

                return ServiceResult<ApproveBookingResponse>.Success(MapBooking<ApproveBookingResponse>(booking));
            }
            catch (Exception ex)
            {
                return ServiceResult<ApproveBookingResponse>.WithError(ex.Message);
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<RejectBookingResponse>> RejectAsync(long bookingId, string? reason, CancellationToken ct)
        {
            try
            {
                var booking = await _bookingRepository.RejectAsync(bookingId, ct);
                if (booking is null)
                    return ServiceResult<RejectBookingResponse>.WithError("Booking not found.");

                return ServiceResult<RejectBookingResponse>.Success(MapBooking<RejectBookingResponse>(booking));
            }
            catch (Exception ex)
            {
                return ServiceResult<RejectBookingResponse>.WithError(ex.Message);
            }
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
