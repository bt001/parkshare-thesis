using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ParkShare.Application.Commands;
using ParkShare.Application.Commands.Validators;
using ParkShare.Application.Queries;
using ParkShare.Application.Queries.Validators;
using ParkShare.Application.Services;
using ParkShare.Common.Constants;
using ParkShare.Common.Enums;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;
using ParkShare.Infrastructure.Services;

internal static partial class Program
{
    /// <summary>Creates a review service over the actual SQL repositories.</summary>
    private static RatingService Ratings(ParkShareDbContext db) => new(new BookingRepository(db), new ListingRepository(db),
        new RatingRepository(db), new RepositoryTransactionCoordinator(db), Clock, NullLogger<RatingService>.Instance);

    /// <summary>Creates an ended confirmed booking without bypassing the completion operation under test.</summary>
    private static async Task<(long Listing, long First, long Second)> EndedFixture()
    {
        var fixture = await Fixture();
        await using var db = Context();
        await db.Bookings.Where(b => b.ListingId == fixture.Listing).ExecuteUpdateAsync(u => u
            .SetProperty(b => b.Status, BookingStatus.Confirmed)
            .SetProperty(b => b.StartDateTime, new DateTime(2026, 9, 12, 14, 0, 0))
            .SetProperty(b => b.EndDateTime, new DateTime(2026, 9, 12, 15, 0, 0))
            .SetProperty(b => b.OwnerResponseAt, new DateTime(2026, 9, 11, 12, 0, 0)));
        return fixture;
    }

    /// <summary>Checks end-time boundaries, fresh ownership, idempotency and notification atomicity.</summary>
    private static async Task Completion()
    {
        var policy = new BookingTimePolicy(Clock);
        var end = new DateTime(2026, 9, 12, 15, 0, 0);
        Check(policy.HasEnded(end) && !policy.HasEnded(end.AddTicks(1)), "completion exact boundary");
        Check(!policy.HasEnded(new(2026, 3, 29, 3, 30, 0)) && !policy.HasEnded(new(2025, 10, 26, 3, 30, 0)), "invalid and ambiguous historical ends rejected");
        Check(!policy.HasEnded(DateTime.SpecifyKind(end, DateTimeKind.Utc)), "UTC wall clock rejected");
        var (listing, first, second) = await EndedFixture();
        await using (var db = Context())
        {
            var service = BookingService(db);
            Check(!(await service.CompleteAsync(first, _renter, Ct)).IsSuccessful, "renter cannot complete");
            Check(!(await service.CompleteAsync(first, long.MaxValue, Ct)).IsSuccessful, "outsider cannot complete");
            Check(!(await service.CompleteAsync(long.MaxValue, _owner, Ct)).IsSuccessful, "missing completion");
            foreach (var status in new[] { BookingStatus.Pending, BookingStatus.Rejected })
            {
                await db.Bookings.Where(b => b.Id == first).ExecuteUpdateAsync(u => u.SetProperty(b => b.Status, status));
                Check((await service.CompleteAsync(first, _owner, Ct)).IsConflict, "wrong completion status");
            }
            await db.Bookings.Where(b => b.Id == first).ExecuteUpdateAsync(u => u.SetProperty(b => b.Status, BookingStatus.Confirmed).SetProperty(b => b.EndDateTime, end.AddSeconds(1)));
            var premature = await new CompleteBookingHandler(service).Handle(new() { BookingId = first, OwnerId = _owner }, Ct);
            Check(premature.IsConflict, "premature completion preserves handler conflict");
            Check(!(await service.GetOwnerByStatusesAsync(_owner, listing, null, 1, 20, Ct)).Payload.Items.Single(b => b.BookingId == first).CanComplete, "server eligibility false before end");
            await db.Bookings.Where(b => b.Id == first).ExecuteUpdateAsync(u => u.SetProperty(b => b.EndDateTime, end));
            await db.Listings.Where(l => l.Id == listing).ExecuteUpdateAsync(u => u.SetProperty(l => l.IsActive, false));
            Check((await service.GetOwnerByStatusesAsync(_owner, listing, null, 1, 20, Ct)).Payload.Items.All(b => b.CanComplete), "inactive listing does not block completion");
        }
        await using (var db = Context(new FailNotificationInsert()))
            Check(!(await BookingService(db).CompleteAsync(first, _owner, Ct)).IsSuccessful, "notification failure returns safe error");
        await using (var db = Context())
        {
            Check((await db.Bookings.SingleAsync(b => b.Id == first)).Status == BookingStatus.Confirmed, "completion rolled back with failed event");
            Check(!await db.Notifications.AnyAsync(n => n.BookingId == first), "no orphan notification");
        }
        await using var one = Context(); await using var two = Context();
        var results = await Task.WhenAll(BookingService(one).CompleteAsync(first, _owner, Ct), BookingService(two).CompleteAsync(first, _owner, Ct));
        Check(results.All(r => r.IsSuccessful && r.Payload.Status == BookingStatus.Completed), "concurrent repeats succeed");
        await using (var db = Context())
        {
            var service = BookingService(db);
            Check((await service.CompleteAsync(first, _owner, Ct)).IsSuccessful, "repeat completed request succeeds");
            Check(!(await service.CompleteAsync(first, _renter, Ct)).IsSuccessful, "idempotency never bypasses ownership");
            var events = await db.Notifications.Where(n => n.BookingId == first).ToListAsync();
            Check(events.Count == 1 && events[0].UserId == _renter && events[0].Type == NotificationTypes.BookingCompleted, "one completion event for renter");
            Check((await db.Bookings.SingleAsync(b => b.Id == first)).OwnerResponseAt == new DateTime(2026, 9, 11, 12, 0, 0), "approval timestamp preserved");
            Check((await service.GetRenterByStatusesAsync(_renter, new() { BookingStatus.Completed }, 1, 100, Ct)).Payload.Items.Any(b => b.BookingId == first), "completed renter history");
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            var propagated = false;
            try { await service.CompleteAsync(second, _owner, cancelled.Token); } catch (OperationCanceledException) { propagated = true; }
            Check(propagated, "completion cancellation propagated");
        }
        Check(!new CompleteBookingCommandValidator().Validate(new CompleteBookingCommand { BookingId = 0, OwnerId = _owner }).IsValid, "invalid completion command");
    }

    /// <summary>Verifies that completion and rating writes wait on the shared listing mutation boundary.</summary>
    private static async Task CompletionRatingLocks()
    {
        var (listing, first, second) = await EndedFixture();
        await using (var db = Context()) await BookingService(db).CompleteAsync(second, _owner, Ct);
        await Race(listing,
            async db => Check((await BookingService(db).CompleteAsync(first, _owner, Ct)).IsSuccessful, "completion waits for listing lock"),
            async db => Check((await Ratings(db).SaveAsync(second, _renter, 4, "Locked", Ct)).IsSuccessful, "rating waits for listing lock"));

        var (freshListing, freshBooking, _) = await EndedFixture();
        await using var blocker = Context();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await new ListingRepository(blocker).LockAsync(freshListing, Ct);
        var signal = new LockSignal();
        await using var request = Context(signal);
        var waiting = BookingService(request).CompleteAsync(freshBooking, _owner, Ct);
        await signal.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
        await blocker.Bookings.Where(b => b.Id == freshBooking).ExecuteUpdateAsync(u => u.SetProperty(b => b.EndDateTime, Future));
        await transaction.CommitAsync();
        Check((await waiting).IsConflict, "completion validates fresh end after lock wait");
    }

    /// <summary>Checks review authorization, serialized upserts, aggregates and pagination across contexts.</summary>
    private static async Task RatingWorkflow()
    {
        var (listing, first, second) = await EndedFixture();
        await using (var db = Context())
        {
            var service = Ratings(db);
            var empty = (await service.GetAsync(listing, 1, 20, Ct)).Payload;
            Check(empty.TotalCount == 0 && empty.AverageStars is null, "empty review aggregate");
            Check((await service.SaveAsync(first, _renter, 5, null, Ct)).IsConflict, "confirmed cannot be rated");
            await BookingService(db).CompleteAsync(first, _owner, Ct);
            await BookingService(db).CompleteAsync(second, _owner, Ct);
            Check(!(await service.SaveAsync(first, _owner, 5, null, Ct)).IsSuccessful, "owner cannot rate");
            Check(!(await service.SaveAsync(first, long.MaxValue, 5, null, Ct)).IsSuccessful, "outsider cannot rate");
            foreach (var stars in new[] { 0, 6 }) Check(!(await service.SaveAsync(first, _renter, stars, null, Ct)).IsSuccessful, "invalid stars");
            Check(!(await service.SaveAsync(first, _renter, 5, new string('x', 1001), Ct)).IsSuccessful, "long comment rejected");
            Check(!(await service.GetAsync(listing, 0, 20, Ct)).IsSuccessful && !(await service.GetAsync(listing, 1, 101, Ct)).IsSuccessful, "read bounds");
            Check(!(await service.GetAsync(long.MaxValue, 1, 20, Ct)).IsSuccessful, "missing listing");
        }
        await using (var one = Context()) await using (var two = Context())
        {
            var results = await Task.WhenAll(Ratings(one).SaveAsync(first, _renter, 3, "one", Ct), Ratings(two).SaveAsync(first, _renter, 5, "two", Ct));
            Check(results.All(r => r.IsSuccessful) && results[0].Payload.Id == results[1].Payload.Id, "concurrent creates upsert the same row");
        }
        long id;
        await using (var db = Context())
        {
            var saved = (await Ratings(db).SaveAsync(first, _renter, 4, "  <b>Great</b>\nParking  ", Ct)).Payload;
            id = saved.Id;
            Check(saved.Comment == "<b>Great</b>\nParking" && saved.CreatedAt == Clock.GetUtcNow().UtcDateTime && saved.CreatedAt.Kind == DateTimeKind.Utc, "trimmed text and original UTC creation");
            await Ratings(db).SaveAsync(second, _renter, 2, null, Ct);
            var all = (await Ratings(db).GetAsync(listing, 1, 1, Ct)).Payload;
            Check(all.TotalCount == 2 && all.AverageStars == 3 && all.Items[0].Id > id, "aggregate covers every review and equal-time sort is stable");
            var next = (await Ratings(db).GetAsync(listing, 2, 1, Ct)).Payload;
            Check(next.Items.Single().Id == id, "stable second page");
            Check((await Ratings(db).GetAsync(listing, int.MaxValue, 100, Ct)).Payload.Items.Count == 0, "large offset safe");
            var history = (await BookingService(db).GetRenterByStatusesAsync(_renter, null, 1, 100, Ct)).Payload;
            Check(history.Items.Single(b => b.BookingId == first).Rating?.Stars == 4, "history includes saved review");
        }
        await using (var one = Context()) await using (var two = Context())
        {
            var results = await Task.WhenAll(Ratings(one).SaveAsync(first, _renter, 1, "edit one", Ct), Ratings(two).SaveAsync(first, _renter, 5, "edit two", Ct));
            Check(results.All(r => r.IsSuccessful && r.Payload.Id == id && r.Payload.CreatedAt == Clock.GetUtcNow().UtcDateTime), "concurrent edits retain identity and creation");
        }
        await using (var db = Context())
        {
            var final = (await Ratings(db).SaveAsync(first, _renter, 5, new string('x', 1000), Ct)).Payload;
            Check(final.Id == id && final.Comment.Length == 1000, "boundary comment saved");
            Check(await db.Ratings.CountAsync(r => r.BookingId == first) == 1, "only one review persisted");
            Check((await Ratings(db).GetAsync(listing, 1, 20, Ct)).Payload.AverageStars == 3.5, "edited aggregate");
            Check(await db.Notifications.CountAsync(n => n.BookingId == first) == 1, "edits do not notify");
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            var propagated = false;
            try { await Ratings(db).SaveAsync(first, _renter, 1, null, cancelled.Token); } catch (OperationCanceledException) { propagated = true; }
            Check(propagated, "rating cancellation propagated");
        }
        Check(!new SaveBookingRatingCommandValidator().Validate(new SaveBookingRatingCommand { BookingId = first, RenterId = _renter, Stars = 6 }).IsValid, "invalid rating command");
        Check(!new GetListingRatingsQueryValidator().Validate(new GetListingRatingsQuery { ListingId = listing, PageSize = 101 }).IsValid, "invalid rating query");
    }
}
