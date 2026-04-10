using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly ParkShareDbContext _db;
        public AvailabilityService(ParkShareDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<ServiceResult<bool>> HasWindowCoverageAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct)
        {
            var covered = await _db.AvailabilityWindows
              .AsNoTracking()
              .AnyAsync(w => w.ListingId == listingId && !w.IsBlocked && w.StartDateTime <= StartDateTime && w.EndDateTime >= EndDateTime, ct);

            return ServiceResult<bool>.Success(covered);
        }

        public async Task<ServiceResult<bool>> HasBlockedOverlapAsync(long listingId, DateTime StartDateTime, DateTime EndDateTime, CancellationToken ct)
        {
            var blocked = await _db.AvailabilityWindows
              .AsNoTracking()
              .AnyAsync(w => w.ListingId == listingId && w.IsBlocked && w.EndDateTime > StartDateTime && w.StartDateTime < EndDateTime, ct);

            return ServiceResult<bool>.Success(blocked);
        }
    }

}
