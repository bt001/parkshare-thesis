using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Services
{
    public class ListingReadService : IListingReadService
    {
        private readonly ParkShareDbContext _db;
        public ListingReadService(ParkShareDbContext db) => _db = db ?? throw new ArgumentNullException(nameof(db));

        public async Task<ServiceResult<ListingCore>> GetCoreAsync(long listingId, CancellationToken ct)
        {
            var core = await _db.Listings
              .AsNoTracking()
              .Where(l => l.Id == listingId)
              .Select(l => new ListingCore
              {
                  Id = l.Id,
                  OwnerId = l.OwnerId,
                  IsActive = l.IsActive,
                  IsAlwaysAvailable = l.IsAlwaysAvailable
              })
              .FirstOrDefaultAsync(ct);

            if (core is null)
                return ServiceResult<ListingCore>.WithError("Listing not found.");

            return ServiceResult<ListingCore>.Success(core);
        }
    }

}
