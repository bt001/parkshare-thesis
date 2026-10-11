using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ParkShare.Application.Commands;
using ParkShare.Application.Commands.Validators;
using ParkShare.Application.Queries;
using ParkShare.Application.Queries.Validators;
using ParkShare.Application.Services;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;

internal static partial class Program
{
    /// <summary>Builds the real participant service on an independent SQL context.</summary>
    private static ChatService Chat(ParkShareDbContext db) => new(new BookingRepository(db), new ChatMessageRepository(db), Clock, NullLogger<ChatService>.Instance);

    /// <summary>Verifies persistence, participant isolation, all statuses and stable bounded paging.</summary>
    private static async Task ChatMessages()
    {
        var (listing, first, second) = await Fixture();
        long outsider;
        await using (var db = Context())
        {
            var user = User("chat-outsider"); db.Users.Add(user); await db.SaveChangesAsync(); outsider = user.Id;
            await db.Listings.Where(l => l.Id == listing).ExecuteUpdateAsync(u => u.SetProperty(l => l.IsActive, false));
            var service = Chat(db);
            foreach (var status in Enum.GetValues<BookingStatus>())
            {
                await db.Bookings.Where(b => b.Id == first).ExecuteUpdateAsync(u => u.SetProperty(b => b.Status, status));
                var owner = await service.SendAsync(first, _owner, "  <b>Hello</b>\nSecond line  ", Ct);
                var renter = await service.SendAsync(first, _renter, new string('x', 2000), Ct);
                Check(owner.IsSuccessful && renter.IsSuccessful, $"both participants can send in {status}");
                Check(owner.Payload.MessageText == "<b>Hello</b>\nSecond line" && owner.Payload.SenderId == _owner, "trimmed text and authenticated sender");
                Check(owner.Payload.SentAt == Clock.GetUtcNow().UtcDateTime, "server clock timestamp");
                Check((await db.Bookings.AsNoTracking().SingleAsync(b => b.Id == first)).Status == status, "chat does not mutate booking status");
            }
            var denied = await service.GetAsync(first, outsider, 1, 50, Ct);
            var missing = await service.GetAsync(long.MaxValue, outsider, 1, 50, Ct);
            Check(!denied.IsSuccessful && denied.ErrorsSummary == missing.ErrorsSummary, "read denies outsider without revealing booking");
            var deniedSend = await service.SendAsync(first, outsider, "forged", Ct);
            var missingSend = await service.SendAsync(long.MaxValue, outsider, "forged", Ct);
            Check(!deniedSend.IsSuccessful && deniedSend.ErrorsSummary == missingSend.ErrorsSummary, "send denies outsider without revealing booking");
            foreach (var invalid in new[] { "", " \r\n ", new string('x', 2001) })
                Check(!(await service.SendAsync(first, _renter, invalid, Ct)).IsSuccessful, "invalid text rejected");
        }
        await using (var db = Context())
        {
            var service = Chat(db);
            var all = (await service.GetAsync(first, _renter, 1, 100, Ct)).Payload;
            Check(all.TotalCount == Enum.GetValues<BookingStatus>().Length * 2, "saved messages survive fresh context and failures add nothing");
            Check(all.Items.All(m => m.SentAt.Kind == DateTimeKind.Utc), "UTC materialization");
            Check(all.Items.Select(m => m.Id).SequenceEqual(all.Items.Select(m => m.Id).OrderDescending()), "equal-time identity ordering");
            var page1 = (await service.GetAsync(first, _owner, 1, 2, Ct)).Payload;
            var page2 = (await service.GetAsync(first, _owner, 2, 2, Ct)).Payload;
            Check(page1.Items.Concat(page2.Items).Select(m => m.Id).SequenceEqual(all.Items.Take(4).Select(m => m.Id)), "stable pages");
            Check((await service.GetAsync(second, _owner, 1, 50, Ct)).Payload.TotalCount == 0, "booking isolation");
            Check((await service.GetAsync(first, _owner, int.MaxValue, 100, Ct)).Payload.Items.Count == 0, "large offset does not overflow");
            Check(!(await service.GetAsync(first, _owner, 0, 50, Ct)).IsSuccessful, "invalid page rejected");
            Check(!(await service.GetAsync(first, _owner, 1, 101, Ct)).IsSuccessful, "oversized page rejected");
            using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
            var propagated = false;
            try { await service.SendAsync(first, _owner, "cancelled", cancellation.Token); }
            catch (OperationCanceledException) { propagated = true; }
            Check(propagated, "caller cancellation propagated");
        }
        var command = new SendBookingMessageCommandValidator();
        Check(command.Validate(new SendBookingMessageCommand { BookingId = first, UserId = _owner, MessageText = " hi " }).IsValid, "valid command");
        Check(!command.Validate(new SendBookingMessageCommand { BookingId = first, UserId = _owner, MessageText = "  " }).IsValid, "blank command rejected");
        var query = new GetBookingMessagesQueryValidator();
        Check(!query.Validate(new GetBookingMessagesQuery { BookingId = first, UserId = _owner, PageSize = 101 }).IsValid, "query page limit");
    }
}
