using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class BookingRepository : IBookingRepository
    {
        private readonly ParkShareDbContext _db;

        /// <summary>
        /// Creates the booking repository.
        /// </summary>
        /// <param name="db">Database context.</param>
        public BookingRepository(ParkShareDbContext db)
            => _db = db ?? throw new ArgumentNullException(nameof(db));

        /// <inheritdoc/>
        public Task<bool> HasConfirmedOverlapAsync(long listingId, DateTime startDateTime, DateTime endDateTime, CancellationToken cancellationToken)
            => _db.Bookings
                .AsNoTracking()
                .AnyAsync(booking => booking.ListingId == listingId
                    && booking.Status == BookingStatus.Confirmed
                    && booking.EndDateTime > startDateTime
                    && booking.StartDateTime < endDateTime, cancellationToken);

        /// <inheritdoc/>
        public async Task AddAsync(Booking booking, CancellationToken cancellationToken)
        {
            await _db.Bookings.AddAsync(booking, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public Task<Booking?> GetByIdWithListingAsync(long bookingId, CancellationToken cancellationToken)
            => _db.Bookings
                .AsNoTracking()
                .Include(booking => booking.Listing)
                .FirstOrDefaultAsync(booking => booking.Id == bookingId, cancellationToken);

        /// <inheritdoc/>
        public Task<long?> GetListingIdAsync(long bookingId, CancellationToken cancellationToken)
            => _db.Bookings.AsNoTracking().Where(b => b.Id == bookingId)
                .Select(b => (long?)b.ListingId).FirstOrDefaultAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<long?> GetOwnerIdAsync(long bookingId, CancellationToken cancellationToken)
            => await _db.Bookings
                .AsNoTracking()
                .Where(booking => booking.Id == bookingId)
                .Select(booking => (long?)booking.Listing.OwnerId)
                .FirstOrDefaultAsync(cancellationToken);

        /// <inheritdoc/>
        public async Task<BookingApprovalResult?> ApproveAndRejectOverlappingPendingAsync(long bookingId, CancellationToken cancellationToken)
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Approval requires the application transaction and listing lock.");
            var booking = await _db.Bookings.Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            if (booking is null) return null;
            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException("Only pending bookings can be approved.");

            var overlaps = await _db.Bookings.AsNoTracking()
                .Where(b => b.ListingId == booking.ListingId && b.Id != booking.Id
                    && b.Status == BookingStatus.Pending && b.EndDateTime > booking.StartDateTime
                    && b.StartDateTime < booking.EndDateTime).ToListAsync(cancellationToken);
            var respondedAt = DateTime.UtcNow;
            booking.Status = BookingStatus.Confirmed;
            booking.OwnerResponseAt = respondedAt;
            await _db.SaveChangesAsync(cancellationToken);
            await _db.Bookings.Where(b => b.ListingId == booking.ListingId && b.Id != booking.Id
                    && b.Status == BookingStatus.Pending && b.EndDateTime > booking.StartDateTime
                    && b.StartDateTime < booking.EndDateTime)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(b => b.Status, BookingStatus.Rejected)
                    .SetProperty(b => b.OwnerResponseAt, respondedAt), cancellationToken);
            return new BookingApprovalResult { Booking = booking, RejectedBookings = overlaps };
        }

        /// <inheritdoc/>
        public async Task<Booking?> RejectAsync(long bookingId, CancellationToken cancellationToken)
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Rejection requires the application transaction and listing lock.");
            var booking = await _db.Bookings.Include(b => b.Listing)
                .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
            if (booking is null) return null;
            if (booking.Status != BookingStatus.Pending)
                throw new InvalidOperationException("Only pending bookings can be rejected.");
            booking.Status = BookingStatus.Rejected;
            booking.OwnerResponseAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return booking;
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Booking>> GetRenterByStatusesAsync(long renterId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _db.Bookings.AsNoTracking().Include(booking => booking.Listing)
                .Where(booking => booking.RenterId == renterId);
            if (statuses != null && statuses.Count > 0)
                query = query.Where(booking => statuses.Contains(booking.Status));

            var total = await query.CountAsync(cancellationToken);
            var offset = ((long)page - 1) * pageSize;
            if (offset >= total)
                return new PagedResult<Booking>(new List<Booking>(), total);

            var items = await query.OrderByDescending(booking => booking.CreatedAt)
                .ThenByDescending(booking => booking.Id)
                .Skip((int)offset).Take(pageSize).ToListAsync(cancellationToken);
            return new PagedResult<Booking>(items, total);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Booking>> GetOwnerByStatusesAsync(long ownerId, long? listingId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = _db.Bookings
                .AsNoTracking()
                .Include(booking => booking.Listing)
                .Where(booking => booking.Listing.OwnerId == ownerId);

            if (listingId.HasValue)
                query = query.Where(booking => booking.ListingId == listingId.Value);

            if (statuses != null && statuses.Count > 0)
                query = query.Where(booking => statuses.Contains(booking.Status));

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(booking => booking.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Booking>(items, total);
        }
    }
}
