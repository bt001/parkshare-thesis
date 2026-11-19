using Microsoft.EntityFrameworkCore;
using ParkShare.Common.Constants;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Repositories;

internal static partial class Program
{
    /// <summary>Checks exact read acknowledgements and role-specific distinct attention counts against SQL.</summary>
    private static async Task Activity()
    {
        var (_, first, second) = await Fixture();
        await using var db = Context();
        var repo = new ActivityRepository(db);
        var beforeOwner = await repo.GetSummaryAsync(_owner, Ct);
        var beforeRenter = await repo.GetSummaryAsync(_renter, Ct);
        var incoming = await Chat(db).SendAsync(first, _owner, "new incoming", Ct);
        var own = await Chat(db).SendAsync(first, _renter, "own reply", Ct);
        Check(incoming.IsSuccessful && own.IsSuccessful, "activity messages saved");
        var incomingId = incoming.Payload.Id;
        Check((await repo.GetSummaryAsync(_renter, Ct)).RenterAttentionCount == beforeRenter.RenterAttentionCount + 1, "incoming chat adds renter attention");
        Check((await repo.GetSummaryAsync(_owner, Ct)).OwnerAttentionCount == beforeOwner.OwnerAttentionCount, "pending plus chat counted once");
        var rows = await repo.GetBookingsAsync(_renter, new Dictionary<long, BookingStatus> { [first] = BookingStatus.Pending }, Ct);
        Check(rows.Count == 1 && rows[first].UnreadMessageCount == 1, "own messages excluded from unread count");
        Check((await Chat(db).MarkReadAsync(second, _renter, new[] { incomingId }, Ct)).Payload.UpdatedCount == 0, "other booking message untouched");
        Check((await Chat(db).MarkReadAsync(first, _owner, new[] { incomingId }, Ct)).Payload.UpdatedCount == 0, "sender cannot read own outgoing message");
        Check(!(await Chat(db).MarkReadAsync(first, long.MaxValue, new[] { incomingId }, Ct)).IsSuccessful, "outsider denied");
        Check(!(await Chat(db).MarkReadAsync(first, _renter, new[] { incomingId, incomingId }, Ct)).IsSuccessful, "duplicate IDs rejected");
        Check(!(await Chat(db).MarkReadAsync(first, _renter, Enumerable.Range(1, 101).Select(i => (long)i).ToArray(), Ct)).IsSuccessful, "oversized acknowledgement rejected");
        var late = await Chat(db).SendAsync(first, _owner, "arrived after display", Ct);
        Check((await Chat(db).MarkReadAsync(first, _renter, new[] { incomingId }, Ct)).Payload.UpdatedCount == 1, "exact displayed message acknowledged");
        Check((await Chat(db).MarkReadAsync(first, _renter, new[] { incomingId }, Ct)).Payload.UpdatedCount == 0, "repeat acknowledgement idempotent");
        Check(await db.ChatMessages.AsNoTracking().AnyAsync(m => m.Id == late.Payload.Id && m.ReadAt == null), "later message remains unread");
        Check((await repo.GetBookingsAsync(_renter, new Dictionary<long, BookingStatus> { [first] = BookingStatus.Pending }, Ct))[first].UnreadMessageCount == 1, "one later message remains counted");
        await Chat(db).MarkReadAsync(first, _renter, new[] { late.Payload.Id }, Ct);
        Check((await repo.GetSummaryAsync(_renter, Ct)).RenterAttentionCount == beforeRenter.RenterAttentionCount, "chat cleared persistently");
        db.Notifications.Add(new Notification { UserId = _renter, BookingId = first, Type = NotificationTypes.BookingApproved, Message = "Decision" });
        await db.SaveChangesAsync();
        var notification = await db.Notifications.SingleAsync(n => n.BookingId == first && n.UserId == _renter);
        Check((await repo.GetBookingsAsync(_renter, new Dictionary<long, BookingStatus> { [first] = BookingStatus.Pending }, Ct))[first].UnreadNotificationIds.Count == 0, "newer decision not acknowledged from stale pending row");
        rows = await repo.GetBookingsAsync(_renter, new Dictionary<long, BookingStatus> { [first] = BookingStatus.Confirmed }, Ct);
        Check(rows[first].UnreadNotificationIds.SequenceEqual(new[] { notification.Id }), "row carries exact notification IDs");
        Check((await repo.GetSummaryAsync(_renter, Ct)).RenterAttentionCount == beforeRenter.RenterAttentionCount + 1, "decision adds attention");
        await Chat(db).SendAsync(first, _owner, "another message", Ct);
        Check((await repo.GetSummaryAsync(_renter, Ct)).RenterAttentionCount == beforeRenter.RenterAttentionCount + 1, "decision plus chat counted once");
        Check((await repo.GetBookingsAsync(long.MaxValue, new Dictionary<long, BookingStatus> { [first] = BookingStatus.Pending }, Ct)).Count == 0, "row metadata participant isolation");
        var page = await BookingService(db).GetRenterByStatusesAsync(_renter, new() { BookingStatus.Pending }, 1, 1, Ct);
        Check(page.IsSuccessful && page.Payload.Items.Count == 1, "paged history enrichment remains bounded");
        var summary = await repo.GetSummaryAsync(long.MaxValue, Ct);
        Check(summary.OwnerAttentionCount == 0 && summary.RenterAttentionCount == 0 && summary.UnreadNotificationCount == 0, "unrelated account sees no activity");
    }
}
