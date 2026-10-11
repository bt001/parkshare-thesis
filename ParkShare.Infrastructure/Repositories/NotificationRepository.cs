using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;

namespace ParkShare.Infrastructure.Repositories
{
    /// <inheritdoc/>
    public class NotificationRepository : INotificationRepository
    {
        private readonly ParkShareDbContext _db;
        /// <summary>Creates persistence using the shared request context.</summary>
        public NotificationRepository(ParkShareDbContext db) => _db = db;

        /// <inheritdoc/>
        public async Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct)
        {
            if (_db.Database.CurrentTransaction is null)
                throw new InvalidOperationException("Booking notifications require the application transaction.");
            await _db.Notifications.AddRangeAsync(notifications, ct);
            await _db.SaveChangesAsync(ct);
        }

        /// <inheritdoc/>
        public async Task<PagedResult<Notification>> GetAsync(long userId, bool unreadOnly, int page, int pageSize, CancellationToken ct)
        {
            var query = _db.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly) query = query.Where(n => !n.IsRead);
            var total = await query.CountAsync(ct);
            var offset = ((long)page - 1) * pageSize;
            if (offset >= total) return new PagedResult<Notification>(new List<Notification>(), total);
            var items = await query.OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Skip((int)offset).Take(pageSize).ToListAsync(ct);
            return new PagedResult<Notification>(items, total);
        }

        /// <inheritdoc/>
        public Task<int> CountUnreadAsync(long userId, CancellationToken ct)
            => _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        /// <inheritdoc/>
        public async Task<bool> MarkReadAsync(long userId, long id, CancellationToken ct)
            => await _db.Notifications.Where(n => n.Id == id && n.UserId == userId)
                .ExecuteUpdateAsync(update => update.SetProperty(n => n.IsRead, true), ct) > 0;
    }
}
