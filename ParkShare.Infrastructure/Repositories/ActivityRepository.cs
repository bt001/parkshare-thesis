using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Constants;
using ParkShare.Common.Enums;
using ParkShare.Infrastructure.Persistence;
namespace ParkShare.Infrastructure.Repositories;

/// <inheritdoc/>
public class ActivityRepository : IActivityRepository
{
    private readonly ParkShareDbContext _db;
    /// <summary>Creates activity queries using the request database context.</summary>
    public ActivityRepository(ParkShareDbContext db) => _db = db;
    /// <inheritdoc/>
    public async Task<ActivitySummaryResponse> GetSummaryAsync(long userId, CancellationToken ct)
    {
        var incoming = _db.ChatMessages.Where(m => m.ReadAt == null && m.SenderId != userId);
        var updates = _db.Notifications.Where(n => n.UserId == userId && !n.IsRead &&
            (n.Type == NotificationTypes.BookingApproved || n.Type == NotificationTypes.BookingRejected ||
             n.Type == NotificationTypes.BookingOverlapRejected || n.Type == NotificationTypes.BookingCompleted));
        return new()
        {
            UnreadNotificationCount = await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, ct),
            OwnerAttentionCount = await _db.Bookings.CountAsync(b => b.Listing.OwnerId == userId &&
                (b.Status == BookingStatus.Pending || incoming.Any(m => m.BookingId == b.Id)), ct),
            RenterAttentionCount = await _db.Bookings.CountAsync(b => b.RenterId == userId &&
                (incoming.Any(m => m.BookingId == b.Id) || updates.Any(n => n.BookingId == b.Id)), ct)
        };
    }
    /// <inheritdoc/>
    public async Task<Dictionary<long, BookingActivityResponse>> GetBookingsAsync(long userId, IReadOnlyDictionary<long, BookingStatus> bookingStatuses, CancellationToken ct)
    {
        var bookingIds = bookingStatuses.Keys.ToArray();
        var accessible = _db.Bookings.Where(b => bookingIds.Contains(b.Id) && (b.RenterId == userId || b.Listing.OwnerId == userId));
        var counts = await accessible.Select(b => new { b.Id, Count = b.ChatMessages.Count(m => m.ReadAt == null && m.SenderId != userId) }).ToListAsync(ct);
        var notifications = await _db.Notifications.Where(n => n.UserId == userId && !n.IsRead && n.BookingId != null &&
            accessible.Any(b => b.Id == n.BookingId)).Select(n => new { n.Id, n.Type, BookingId = n.BookingId!.Value }).ToListAsync(ct);
        return counts.ToDictionary(b => b.Id, b => new BookingActivityResponse
        {
            UnreadMessageCount = b.Count,
            UnreadNotificationIds = notifications.Where(n => n.BookingId == b.Id &&
                (n.Type == NotificationTypes.BookingRequested ||
                 (n.Type == NotificationTypes.BookingApproved && (bookingStatuses[b.Id] == BookingStatus.Confirmed || bookingStatuses[b.Id] == BookingStatus.Completed)) ||
                 ((n.Type == NotificationTypes.BookingRejected || n.Type == NotificationTypes.BookingOverlapRejected) && bookingStatuses[b.Id] == BookingStatus.Rejected) ||
                 (n.Type == NotificationTypes.BookingCompleted && bookingStatuses[b.Id] == BookingStatus.Completed))).Select(n => n.Id).ToList()
        });
    }
}
