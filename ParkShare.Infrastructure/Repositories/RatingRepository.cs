using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class RatingRepository : IRatingRepository
    {
        private readonly ParkShareDbContext _db;
        /// <summary>Creates the repository.</summary>
        public RatingRepository(ParkShareDbContext db) => _db = db;
        /// <inheritdoc/>
        public async Task<Rating> SaveAsync(Rating rating, CancellationToken ct)
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Rating writes require the application transaction and listing lock.");
            var existing = await _db.Ratings.AsNoTracking().SingleOrDefaultAsync(r => r.BookingId == rating.BookingId, ct);
            if (existing is null)
            {
                _db.Ratings.Add(rating);
                await _db.SaveChangesAsync(ct);
                return rating;
            }
            await _db.Ratings.Where(r => r.Id == existing.Id).ExecuteUpdateAsync(update => update
                .SetProperty(r => r.Stars, rating.Stars).SetProperty(r => r.Comment, rating.Comment), ct);
            existing.Stars = rating.Stars; existing.Comment = rating.Comment;
            return existing;
        }
        /// <inheritdoc/>
        public async Task<PagedResult<Rating>> GetAsync(long listingId, int page, int pageSize, CancellationToken ct)
        {
            var query = _db.Ratings.AsNoTracking().Where(r => r.ListingId == listingId);
            var count = await query.CountAsync(ct);
            var offset = ((long)page - 1) * pageSize;
            var items = offset >= count ? new List<Rating>() : await query.OrderByDescending(r => r.CreatedAt)
                .ThenByDescending(r => r.Id).Skip((int)offset).Take(pageSize).ToListAsync(ct);
            return new PagedResult<Rating>(items, count);
        }
        /// <inheritdoc/>
        public Task<double?> AverageAsync(long listingId, CancellationToken ct)
            => _db.Ratings.Where(r => r.ListingId == listingId).AverageAsync(r => (double?)r.Stars, ct);
    }
}
