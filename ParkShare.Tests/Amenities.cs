using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ParkShare.Application.Commands;
using ParkShare.Application.Commands.Validators;
using ParkShare.Application.Services;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;
using ParkShare.Infrastructure.Services;

internal static partial class Program
{
    /// <summary>Creates the listing service with SQL repositories.</summary>
    private static ListingService Listings(ParkShareDbContext db) => new(new UserRepository(db), new UserRoleRepository(db),
        new ListingRepository(db), new RepositoryTransactionCoordinator(db), NullLogger<ListingService>.Instance,
        new AmenityRepository(db), new ListingAmenityRepository(db));

    /// <summary>Checks catalog reads and complete owner selection workflows.</summary>
    private static async Task AmenityWorkflow()
    {
        long id; long[] ids;
        await using (var db = Context())
        {
            var catalog = await new AmenityService(new AmenityRepository(db), NullLogger<AmenityService>.Instance).GetAllAsync(Ct);
            Check(catalog.IsSuccessful && catalog.Payload.Items.Count == 5, "catalog available");
            Check(catalog.Payload.Items.Select(a => a.Name).SequenceEqual(catalog.Payload.Items.Select(a => a.Name).OrderBy(n => n)), "catalog sorted");
            ids = catalog.Payload.Items.Take(2).Select(a => a.Id).ToArray();
            var result = await Listings(db).CreateAsync(_owner, "Amenity space", "Details", 10, 44, 26, null, null, true, Ct, ids);
            Check(result.IsSuccessful, "create selected amenities"); id = result.Payload.ListingId;
        }
        await using (var db = Context())
        {
            var service = Listings(db);
            Check((await service.GetByIdAsync(id, Ct)).Payload.Amenities.Select(a => a.Id).SequenceEqual(ids), "detail persists selections across contexts");
            Check(!(await service.UpdateAsync(id, _renter, "Unauthorized", "Details", 10, 44, 26, null, null, true, Ct, [])).IsSuccessful, "non-owner rejected");
            foreach (var invalid in new[] { new long[] { 0 }, new[] { ids[0], ids[0] }, new[] { long.MaxValue } })
            {
                Check(!(await service.UpdateAsync(id, _owner, "Invalid", "Details", 10, 44, 26, null, null, true, Ct, invalid)).IsSuccessful, "invalid selections rejected");
                Check(!(await service.CreateAsync(_owner, "Invalid create", "Details", 10, 44, 26, null, null, true, Ct, invalid)).IsSuccessful, "invalid create rejected");
            }
            Check((await db.Listings.AsNoTracking().SingleAsync(l => l.Id == id)).Title == "Amenity space", "invalid edit preserves core fields");
            Check(await db.ListingAmenities.CountAsync(a => a.ListingId == id) == 2, "invalid edit preserves selections");
            Check((await service.UpdateAsync(id, _owner, "Preserved", "Details", 10, 44, 26, null, null, true, Ct)).IsSuccessful, "omitted update accepted");
            Check(await db.ListingAmenities.CountAsync(a => a.ListingId == id) == 2, "omitted selections preserved");
            Check((await service.DeactivateAsync(id, _owner, Ct)).IsSuccessful, "deactivate fixture");
            Check((await service.UpdateAsync(id, _owner, "Reduced", "Details", 10, 44, 26, null, null, true, Ct, new[] { ids[1] })).IsSuccessful, "inactive listing editable");
            Check((await service.GetByIdAsync(id, Ct)).Payload.Amenities.Single().Id == ids[1], "remove one selection");
            Check((await service.UpdateAsync(id, _owner, "Cleared", "Details", 10, 44, 26, null, null, true, Ct, [])).IsSuccessful, "clear all");
            Check((await service.GetByIdAsync(id, Ct)).Payload.Amenities.Count == 0, "empty detail");
            var empty = await service.CreateAsync(_owner, "No amenities", "Details", 10, 44, 26, null, null, true, Ct);
            Check(empty.IsSuccessful && (await service.GetByIdAsync(empty.Payload.ListingId, Ct)).Payload.Amenities.Count == 0, "old create contract supported");
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            var propagated = false;
            try { await service.CreateAsync(_owner, "Cancelled", "Details", 10, 44, 26, null, null, true, cancelled.Token, ids); }
            catch (OperationCanceledException) { propagated = true; }
            Check(propagated, "caller cancellation propagated");
        }
        Check(!new CreateListingCommandValidator().Validate(new CreateListingCommand { OwnerId = _owner, Title = "Space", Description = "Details", PricePerHour = 10, AmenityIds = [1, 1] }).IsValid, "create validator rejects duplicate IDs");
        Check(!new UpdateListingCommandValidator().Validate(new UpdateListingCommand { Id = id, OwnerId = _owner, Title = "Space", Description = "Details", PricePerHour = 10, AmenityIds = [-1] }).IsValid, "update validator rejects invalid IDs");
    }

    /// <summary>Checks atomic failure, serialized replacements and fresh ownership after waiting.</summary>
    private static async Task AmenityTransactions()
    {
        var (listing, _, _) = await Fixture();
        long[] ids;
        await using (var db = Context()) ids = await db.Amenities.OrderBy(a => a.Id).Select(a => a.Id).Take(2).ToArrayAsync();
        await using (var db = Context(new FailAmenityInsert()))
            Check(!(await Listings(db).UpdateAsync(listing, _owner, "Must rollback", "Details", 10, 44, 26, null, null, true, Ct, ids)).IsSuccessful, "failed association returns safe error");
        await using (var db = Context())
        {
            Check((await db.Listings.SingleAsync(l => l.Id == listing)).Title == "Test space", "core edit rolled back");
            Check(!await db.ListingAmenities.AnyAsync(a => a.ListingId == listing), "no partial associations");
        }
        await using (var db = Context(new FailAmenityInsert()))
            Check(!(await Listings(db).CreateAsync(_renter, "Rollback create", "Details", 10, 44, 26, null, null, true, Ct, ids)).IsSuccessful, "create association failure");
        await using (var db = Context())
        {
            Check(!await db.Listings.AnyAsync(l => l.Title == "Rollback create"), "new listing rolled back");
            Check(!await db.UserRoles.AnyAsync(r => r.UserId == _renter && r.RoleId == (long)ParkShare.Common.Enums.Roles.Owner), "new owner role rolled back");
        }
        await Race(listing,
            async db => Check((await Listings(db).UpdateAsync(listing, _owner, "First", "Details", 10, 44, 26, null, null, true, Ct, new[] { ids[0] })).IsSuccessful, "first replacement"),
            async db => Check((await Listings(db).UpdateAsync(listing, _owner, "Second", "Details", 10, 44, 26, null, null, true, Ct, new[] { ids[1] })).IsSuccessful, "second replacement"));
        await using (var db = Context())
        {
            var row = await db.Listings.SingleAsync(l => l.Id == listing);
            var selected = await db.ListingAmenities.SingleAsync(a => a.ListingId == listing);
            Check(selected.AmenityId == (row.Title == "First" ? ids[0] : ids[1]), "last committed core and selection agree");
        }
        await using var blocker = Context();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await new ListingRepository(blocker).LockAsync(listing, Ct);
        var probe = new LockSignal(); await using var waitingDb = Context(probe);
        var waiting = Listings(waitingDb).UpdateAsync(listing, _owner, "Stale owner", "Details", 10, 44, 26, null, null, true, Ct, []);
        await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Check(!waiting.IsCompleted, "selection update waits on listing lock");
        await blocker.Listings.Where(l => l.Id == listing).ExecuteUpdateAsync(u => u.SetProperty(l => l.OwnerId, _renter));
        await transaction.CommitAsync();
        Check(!(await waiting).IsSuccessful, "ownership rechecked after lock");
    }

    /// <summary>Fails selection insertion to exercise the application transaction rollback.</summary>
    private sealed class FailAmenityInsert : DbCommandInterceptor
    {
        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("ListingAmenities") && (command.CommandText.Contains("INSERT") || command.CommandText.Contains("MERGE")))
                throw new InvalidOperationException("Injected amenity failure.");
            return ValueTask.FromResult(result);
        }
    }
}
