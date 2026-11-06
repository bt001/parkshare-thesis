using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using ParkShare.Application.Services;
using ParkShare.Common.Constants;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;

/// <summary>Real SQL notification and migration regressions.</summary>
internal static partial class Program
{
    /// <summary>Upgrades the previous schema with an existing notification intact.</summary>
    private static async Task VerifyNotificationMigration(ParkShareDbContext db)
    {
        await db.GetService<IMigrator>().MigrateAsync("20260502190000_InitialCreate");
        var legacyUser = User("legacy");
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Users (UserName, Email, FirstName, LastName, PasswordHash, CreatedAt, IsActive) VALUES ({legacyUser.UserName}, {legacyUser.Email}, {legacyUser.FirstName}, {legacyUser.LastName}, {legacyUser.PasswordHash}, {legacyUser.CreatedAt}, {legacyUser.IsActive})");
        legacyUser.Id = await db.Database.SqlQuery<long>($"SELECT Id AS Value FROM Users WHERE Email = {legacyUser.Email}").SingleAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Notifications (UserId, Type, Message, IsRead, CreatedAt) VALUES ({legacyUser.Id}, {"legacy-event"}, {"Keep this message"}, {true}, {new DateTime(2026, 1, 1)})");
        var preservedAmenity = new Amenity { Name = "Lighting", Description = "Existing description" };
        var preservedListing = new Listing { OwnerId = legacyUser.Id, Title = "Legacy amenity listing", Description = "Preserve", PricePerHour = 1 };
        db.Amenities.Add(preservedAmenity); db.Listings.Add(preservedListing); await db.SaveChangesAsync();
        db.ListingAmenities.Add(new() { ListingId = preservedListing.Id, AmenityId = preservedAmenity.Id }); await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Check(await db.Amenities.CountAsync(a => a.Name == "Lighting") == 1, "catalog migration does not duplicate existing names");
        Check(await db.Amenities.CountAsync() == 5, "catalog inserts missing defaults");
        Check((await db.Amenities.AsNoTracking().SingleAsync(a => a.Id == preservedAmenity.Id)).Description == "Existing description", "catalog preserves existing description and ID");
        Check(await db.ListingAmenities.AnyAsync(a => a.ListingId == preservedListing.Id && a.AmenityId == preservedAmenity.Id), "catalog preserves legacy association");
        await db.Database.MigrateAsync();
        Check(await db.Amenities.CountAsync() == 5, "migration rerun remains idempotent");
        var legacy = await db.Notifications.SingleAsync();
        Check(legacy.BookingId == null && legacy.Type == "legacy-event" && legacy.Message == "Keep this message" && legacy.IsRead, "migration preserves legacy event");
        Check(!db.Database.HasPendingModelChanges(), "migration matches current model");
        _passed++; Console.WriteLine("PASS additive notification migration preserves legacy rows");
    }

    /// <summary>Verifies all four event types, recipients, boundaries, paging and read state.</summary>
    private static async Task Notifications()
    {
        var (listing, first, second) = await Fixture();
        long otherId, thirdId, separateId, requestedId;
        await using (var db = Context())
        {
            var other = User("notification-renter"); db.Users.Add(other); await db.SaveChangesAsync(); otherId = other.Id;
            var third = new Booking { ListingId = listing, RenterId = other.Id, StartDateTime = Future, EndDateTime = Future.AddHours(1) };
            var separate = new Booking { ListingId = listing, RenterId = _renter, StartDateTime = Future.AddHours(1), EndDateTime = Future.AddHours(2) };
            db.Bookings.AddRange(third, separate); await db.SaveChangesAsync(); thirdId = third.Id; separateId = separate.Id;
        }
        await using (var db = Context())
        {
            var result = await BookingService(db).CreateAsync(new() { ListingId = listing, RenterId = _renter, StartDateTime = Future.AddDays(1), EndDateTime = Future.AddDays(1).AddHours(1) }, Ct);
            Check(result.IsSuccessful, "request created with notification"); requestedId = result.Payload.BookingId;
        }
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "approval emits notifications");
        await using (var db = Context()) Check((await BookingService(db).RejectAsync(requestedId, _owner, null, Ct)).IsSuccessful, "manual rejection emits notification");
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "repeat approval succeeds");
        await using (var db = Context()) Check((await BookingService(db).RejectAsync(requestedId, _owner, null, Ct)).IsSuccessful, "repeat rejection succeeds");
        await using var check = Context();
        var events = await check.Notifications.Where(n => n.Booking!.ListingId == listing).ToListAsync();
        Check(events.Count == 5, "exact event count including distinct events on requested booking");
        Check(events.Single(n => n.Type == NotificationTypes.BookingRequested).UserId == _owner, "request recipient is owner");
        Check(events.Single(n => n.Type == NotificationTypes.BookingApproved).BookingId == first, "approved reference");
        Check(events.Single(n => n.Type == NotificationTypes.BookingRejected).BookingId == requestedId, "rejected reference");
        Check(events.Single(n => n.BookingId == second).UserId == _renter, "first overlap renter notified");
        Check(events.Single(n => n.BookingId == thirdId).UserId == otherId, "other overlap renter notified");
        Check(events.Count(n => n.Type == NotificationTypes.BookingOverlapRejected) == 2, "one overlap event per affected booking");
        Check((await check.Bookings.FindAsync(separateId))!.Status == BookingStatus.Pending && !events.Any(n => n.BookingId == separateId), "touching non-overlap unchanged");
        var repository = new NotificationRepository(check);
        var service = new NotificationService(repository, NullLogger<NotificationService>.Instance);
        var isolated = await repository.GetAsync(otherId, false, 1, 20, Ct);
        Check(isolated.TotalCount == 1 && isolated.Items.Single().BookingId == thirdId, "recipient isolation");
        var id = isolated.Items.Single().Id;
        var missing = await service.MarkReadAsync(_owner, id, Ct);
        var nonexistent = await service.MarkReadAsync(_owner, long.MaxValue, Ct);
        Check(!missing.IsSuccessful && missing.ErrorsSummary == nonexistent.ErrorsSummary, "outsider and missing indistinguishable");
        Check(await repository.CountUnreadAsync(otherId, Ct) == 1, "unread count");
        Check((await service.MarkReadAsync(otherId, id, Ct)).IsSuccessful, "mark read succeeds");
        Check((await service.MarkReadAsync(otherId, id, Ct)).IsSuccessful && await repository.CountUnreadAsync(otherId, Ct) == 0, "repeat read remains zero");
        Check((await repository.GetAsync(otherId, true, 1, 20, Ct)).TotalCount == 0, "unread filter");
        await check.Notifications.Where(n => n.UserId == _renter).ExecuteUpdateAsync(u => u.SetProperty(n => n.CreatedAt, new DateTime(2026, 1, 1)));
        var all = await repository.GetAsync(_renter, false, 1, 100, Ct);
        var page1 = await repository.GetAsync(_renter, false, 1, 2, Ct);
        var page2 = await repository.GetAsync(_renter, false, 2, 2, Ct);
        Check(page1.Items.Concat(page2.Items).Select(n => n.Id).SequenceEqual(all.Items.Take(4).Select(n => n.Id)), "tie-broken stable pages");
        Check((await repository.GetAsync(_renter, false, int.MaxValue, 100, Ct)).Items.Count == 0, "large page does not overflow");
        Check((await service.GetAsync(otherId, false, 1, 20, Ct)).Payload.Items.Single().CreatedAt.Kind == DateTimeKind.Utc, "UTC timestamp contract");
        // Database uniqueness remains a final defense if a future caller bypasses decision idempotency.
        var duplicate = events.First();
        check.Notifications.Add(new Notification { UserId = duplicate.UserId, BookingId = duplicate.BookingId, Type = duplicate.Type });
        var prevented = false;
        try { await check.SaveChangesAsync(); } catch (DbUpdateException) { prevented = true; }
        Check(prevented, "duplicate booking event rejected by unique index");
    }

    /// <summary>Fails notification insertion after booking writes and verifies complete rollback.</summary>
    private static async Task NotificationRollback()
    {
        var (listing, first, _) = await Fixture();
        foreach (var operation in new[] { "request", "approve", "reject" })
        {
            await using (var db = Context(new FailNotificationInsert()))
            {
                var service = BookingService(db);
                var success = operation switch
                {
                    "request" => (await service.CreateAsync(new() { ListingId = listing, RenterId = _renter, StartDateTime = Future.AddDays(2), EndDateTime = Future.AddDays(2).AddHours(1) }, Ct)).IsSuccessful,
                    "approve" => (await service.ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful,
                    _ => (await service.RejectAsync(first, _owner, null, Ct)).IsSuccessful
                };
                Check(!success, "injected notification failure is safe");
            }
            await using var check = Context();
            Check(await check.Bookings.CountAsync(b => b.ListingId == listing) == 2, "failed request rolled back");
            Check(await check.Bookings.Where(b => b.ListingId == listing).AllAsync(b => b.Status == BookingStatus.Pending && b.OwnerResponseAt == null), "decision and overlaps rolled back");
            Check(!await check.Notifications.AnyAsync(n => n.Booking!.ListingId == listing), "no orphan events");
        }
    }

    /// <summary>Checks repeated simultaneous approvals emit one terminal event per booking.</summary>
    private static async Task NotificationConcurrency()
    {
        var (listing, first, _) = await Fixture();
        await Race(listing,
            async db => Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "first repeated decision"),
            async db => Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "second repeated decision"));
        await using var check = Context();
        Check(await check.Notifications.CountAsync(n => n.Booking!.ListingId == listing) == 2, "one approval and one overlap event");
    }

    /// <summary>Injects failure at SQL event insertion after prior booking writes.</summary>
    private sealed class FailNotificationInsert : DbCommandInterceptor
    {
        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("Notifications") && (command.CommandText.Contains("INSERT") || command.CommandText.Contains("MERGE")))
                throw new InvalidOperationException("Injected notification failure.");
            return ValueTask.FromResult(result);
        }
    }
}
