using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkShare.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly ParkShareDbContext _db;
        public BookingService(ParkShareDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<ServiceResult<bool>> HasConfirmedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct)
        {
            var overlap = await _db.Bookings
              .AsNoTracking()
              .AnyAsync(b => b.ListingId == listingId && b.Status == BookingStatus.Confirmed && b.EndDateTime > StartDateTime && b.StartDateTime < EndDateTime, ct);

            return ServiceResult<bool>.Success(overlap);
        }

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
                await _db.Bookings.AddAsync(entity, ct);
                await _db.SaveChangesAsync(ct);

                var ownerId = await _db.Listings.AsNoTracking().Where(l => l.Id == entity.ListingId).Select(l => l.OwnerId).FirstOrDefaultAsync(ct);

                var payload = new CreateBookingResponse
                {
                    BookingId = entity.Id,
                    ListingId = entity.ListingId,
                    RenterId = entity.RenterId,
                    OwnerId = ownerId,
                    StartDateTime = entity.StartDateTime,
                    EndDateTime = entity.EndDateTime,
                    Status = entity.Status,
                    CreatedAt = entity.CreatedAt
                };

                return ServiceResult<CreateBookingResponse>.Success(payload);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<CreateBookingResponse>.WithError("Failed to create booking.");
            }
        }

        public async Task<ServiceResult<GetBookingByIdResponse>> GetByIdAsync(long bookingId, CancellationToken ct)
        {
            var result = await _db.Bookings
              .AsNoTracking()
              .Where(b => b.Id == bookingId)
              .Select(b => new
              {
                  b.Id,
                  b.ListingId,
                  b.RenterId,
                  b.StartDateTime,
                  b.EndDateTime,
                  b.Status,
                  b.CreatedAt,
                  OwnerId = _db.Listings.Where(l => l.Id == b.ListingId).Select(l => l.OwnerId).FirstOrDefault()
              })
              .FirstOrDefaultAsync(ct);

            if (result is null)
                return ServiceResult<GetBookingByIdResponse>.WithError("Booking not found.");

            var payload = new GetBookingByIdResponse
            {
                BookingId = result.Id,
                ListingId = result.ListingId,
                RenterId = result.RenterId,
                OwnerId = result.OwnerId,
                StartDateTime = result.StartDateTime,
                EndDateTime = result.EndDateTime,
                Status = result.Status,
                CreatedAt = result.CreatedAt
            };

            return ServiceResult<GetBookingByIdResponse>.Success(payload);
        }

        public async Task<ServiceResult<long>> GetOwnerForBookingAsync(long bookingId, CancellationToken ct)
        {
            var ownerId = await _db.Bookings
              .AsNoTracking()
              .Where(b => b.Id == bookingId)
              .Select(b => _db.Listings.Where(l => l.Id == b.ListingId).Select(l => l.OwnerId).FirstOrDefault())
              .FirstOrDefaultAsync(ct);

            if (ownerId == 0)
                return ServiceResult<long>.WithError("Booking not found.");

            return ServiceResult<long>.Success(ownerId);
        }

        public async Task<ServiceResult<ApproveBookingResponse>> ApproveAndRejectOverlappingPendingAsync(long bookingId, CancellationToken ct)
        {
            using var tx = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                if (b is null)
                    return ServiceResult<ApproveBookingResponse>.WithError("Booking not found.");

                if (b.Status == BookingStatus.Confirmed)
                {
                    var ownerIdConfirmed = await _db.Listings.AsNoTracking().Where(l => l.Id == b.ListingId).Select(l => l.OwnerId).FirstOrDefaultAsync(ct);
                    var already = new ApproveBookingResponse
                    {
                        BookingId = b.Id,
                        ListingId = b.ListingId,
                        RenterId = b.RenterId,
                        OwnerId = ownerIdConfirmed,
                        StartDateTime = b.StartDateTime,
                        EndDateTime = b.EndDateTime,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    };
                    return ServiceResult<ApproveBookingResponse>.Success(already);
                }

                b.Status = BookingStatus.Confirmed;
                b.OwnerResponseAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                await _db.Bookings
                  .Where(x => x.ListingId == b.ListingId && x.Id != b.Id && x.Status == BookingStatus.Pending && x.EndDateTime > b.StartDateTime && x.StartDateTime < b.EndDateTime)
                  .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, BookingStatus.Rejected), ct);

                await tx.CommitAsync(ct);

                var ownerId = await _db.Listings.AsNoTracking().Where(l => l.Id == b.ListingId).Select(l => l.OwnerId).FirstOrDefaultAsync(ct);

                var payload = new ApproveBookingResponse
                {
                    BookingId = b.Id,
                    ListingId = b.ListingId,
                    RenterId = b.RenterId,
                    OwnerId = ownerId,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                };

                return ServiceResult<ApproveBookingResponse>.Success(payload);
            }
            catch (DbUpdateException)
            {
                await tx.RollbackAsync(ct);
                return ServiceResult<ApproveBookingResponse>.WithError("Failed to approve booking.");
            }
        }

        public async Task<ServiceResult<RejectBookingResponse>> RejectAsync(long bookingId, string? reason, CancellationToken ct)
        {
            try
            {
                var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, ct);
                if (b is null)
                    return ServiceResult<RejectBookingResponse>.WithError("Booking not found.");

                if (b.Status == BookingStatus.Rejected)
                {
                    var ownerIdExisting = await _db.Listings.AsNoTracking().Where(l => l.Id == b.ListingId).Select(l => l.OwnerId).FirstOrDefaultAsync(ct);
                    var already = new RejectBookingResponse
                    {
                        BookingId = b.Id,
                        ListingId = b.ListingId,
                        RenterId = b.RenterId,
                        OwnerId = ownerIdExisting,
                        StartDateTime = b.StartDateTime,
                        EndDateTime = b.EndDateTime,
                        Status = b.Status,
                        CreatedAt = b.CreatedAt
                    };
                    return ServiceResult<RejectBookingResponse>.Success(already);
                }

                b.Status = BookingStatus.Rejected;
                b.OwnerResponseAt = DateTime.UtcNow;
                // Optionally persist reason if your Booking entity has a column

                await _db.SaveChangesAsync(ct);

                var ownerId = await _db.Listings.AsNoTracking().Where(l => l.Id == b.ListingId).Select(l => l.OwnerId).FirstOrDefaultAsync(ct);

                var payload = new RejectBookingResponse
                {
                    BookingId = b.Id,
                    ListingId = b.ListingId,
                    RenterId = b.RenterId,
                    OwnerId = ownerId,
                    StartDateTime = b.StartDateTime,
                    EndDateTime = b.EndDateTime,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt
                };

                return ServiceResult<RejectBookingResponse>.Success(payload);
            }
            catch (DbUpdateException)
            {
                return ServiceResult<RejectBookingResponse>.WithError("Failed to reject booking.");
            }
        }

        public async Task<ServiceResult<GetOwnerBookingsResponse>> GetOwnerByStatusesAsync(long ownerId, long? listingId, List<BookingStatus>? statuses, int page, int pageSize, CancellationToken ct)
        {
            var q = from b in _db.Bookings.AsNoTracking()
                    join l in _db.Listings.AsNoTracking() on b.ListingId equals l.Id
                    where l.OwnerId == ownerId
                    select new { b.Id, b.ListingId, l.Title, b.RenterId, b.StartDateTime, b.EndDateTime, b.Status, b.CreatedAt };

            if (listingId.HasValue)
                q = q.Where(x => x.ListingId == listingId.Value);

            if (statuses != null && statuses.Count > 0)
                q = q.Where(x => statuses.Contains(x.Status));

            var total = await q.CountAsync(ct);

            var items = await q
              .OrderByDescending(x => x.CreatedAt)
              .Skip((page - 1) * pageSize)
              .Take(pageSize)
              .Select(x => new OwnerBookingItem
              {
                  BookingId = x.Id,
                  ListingId = x.ListingId,
                  ListingTitle = x.Title,
                  RenterId = x.RenterId,
                  StartUtc = x.StartDateTime,
                  EndUtc = x.EndDateTime,
                  Status = x.Status,
                  CreatedAt = x.CreatedAt
              })
              .ToListAsync(ct);

            var payload = new GetOwnerBookingsResponse { Page = page, PageSize = pageSize, TotalCount = total, Items = items };
            return ServiceResult<GetOwnerBookingsResponse>.Success(payload);
        }
    }

}
