using ParkShare.Application.Dto.Infrastructure;
using ParkShare.Domain.Entities;

namespace ParkShare.Application.Interfaces.Repositories
{
    /// <summary>Recipient-scoped notification persistence.</summary>
    public interface INotificationRepository
    {
        /// <summary>Persists events within the caller's booking transaction.</summary>
        Task AddRangeAsync(IEnumerable<Notification> notifications, CancellationToken ct);
        /// <summary>Returns a stable page belonging only to the recipient.</summary>
        Task<PagedResult<Notification>> GetAsync(long userId, bool unreadOnly, int page, int pageSize, CancellationToken ct);
        /// <summary>Counts the recipient's unread notifications.</summary>
        Task<int> CountUnreadAsync(long userId, CancellationToken ct);
        /// <summary>Marks a recipient-owned notification read, returning false when absent.</summary>
        Task<bool> MarkReadAsync(long userId, long id, CancellationToken ct);
    }
}
