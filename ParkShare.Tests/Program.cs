using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto;
using ParkShare.Application.Services;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Persistence;
using ParkShare.Infrastructure.Repositories;
using ParkShare.Infrastructure.Services;

/// <summary>Executable regression suite using isolated SQL Server data and independent request contexts.</summary>
internal static partial class Program
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly DateTime Future = new(2030, 1, 15, 10, 0, 0);
    private static readonly FixedClock Clock = new();
    private static string _connection = string.Empty;
    private static long _owner;
    private static long _renter;
    private static int _passed;

    /// <summary>Runs clock tests and SQL Server regressions, deleting only this run's generated database.</summary>
    private static async Task<int> Main()
    {
        try { await RunAsync(); return 0; }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    /// <summary>Runs all regression groups with cleanup even when an assertion fails.</summary>
    private static async Task RunAsync()
    {
        TestTimePolicy();
        var database = "ParkShare_Tests_" + Guid.NewGuid().ToString("N");
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("PARKSHARE_TEST_SQL")
            ?? "Server=SPEC-PC1\\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True;")
        { InitialCatalog = database };
        _connection = builder.ConnectionString;
        try
        {
            await using (var db = Context())
            {
                await VerifyNotificationMigration(db);
                var owner = User("owner"); var renter = User("renter");
                db.Users.AddRange(owner, renter); await db.SaveChangesAsync();
                _owner = owner.Id; _renter = renter.Id;
            }
            await Test("transitions, ownership, timestamps, handler conflict propagation", Transitions);
            await Test("past create and expired approval", PastPeriods);
            await Test("clock advances while waiting for listing lock", TimeAfterLock);
            await Test("overlapping approvals serialize", ConcurrentApprovals);
            await Test("approve versus reject serialize", ConcurrentDecisions);
            await Test("approval versus blocked window serialize", ConcurrentBlock);
            await Test("failed overlap update rolls back all writes", Rollback);
            await Test("listing and availability mutations wait for listing lock", MutationLocks);
            await Test("notification recipients, pagination and idempotency", Notifications);
            await Test("notification write failure rolls back every booking mutation", NotificationRollback);
            await Test("concurrent repeated decisions emit one event", NotificationConcurrency);
            await Test("chat participation, persistence, statuses, validation and paging", ChatMessages);
            await Test("completion time, ownership, concurrency and atomic notifications", Completion);
            await Test("editable ratings, isolation, concurrency, aggregates and paging", RatingWorkflow);
            await Test("completion and rating listing locks and fresh checks", CompletionRatingLocks);
            await Test("amenity catalog, create/edit persistence, identity and validation", AmenityWorkflow);
            await Test("amenity rollback, listing locks and concurrent replacement", AmenityTransactions);
            await Test("photo persistence, bounds, isolation and private storage", PhotoWorkflow);
            await Test("photo concurrency, fresh ownership, rollback and cleanup failures", PhotoTransactions);
            await Test("place provider configuration, cache, budget and safe failures", PlaceSearch);
            await Test("search periods, geography, paging and fresh booking checks", SearchAvailability);
            await Test("search quality projections, filters, sorting and estimates", SearchQuality);
            await Test("viewport bounds, edges, wrapping and pagination", SearchViewport);
            Console.WriteLine($"PASS: {_passed} regression groups.");
        }
        finally
        {
            if (!database.StartsWith("ParkShare_Tests_", StringComparison.Ordinal)
                || new SqlConnectionStringBuilder(_connection).InitialCatalog != database)
                throw new InvalidOperationException("Refusing cleanup outside this run's test database.");
            await using var cleanup = Context();
            await cleanup.Database.EnsureDeletedAsync();
        }
    }

    /// <summary>Checks Romanian wall-clock boundaries without relying on the host time zone.</summary>
    private static void TestTimePolicy()
    {
        var policy = new BookingTimePolicy(Clock);
        var boundary = new DateTime(2026, 9, 12, 15, 0, 0);
        Check(policy.Validate(boundary, boundary.AddHours(1)) != null, "exact now rejected");
        Check(policy.Validate(boundary.AddTicks(1), boundary.AddHours(1)) == null, "future tick accepted");
        Check(policy.Validate(boundary.AddHours(-1), boundary) != null, "past rejected");
        Check(policy.Validate(Future, Future) != null, "empty period rejected");
        Check(policy.Validate(new(2030, 3, 31, 3, 30, 0), new(2030, 3, 31, 5, 0, 0)) != null, "DST gap rejected");
        Check(policy.Validate(new(2030, 10, 27, 3, 30, 0), new(2030, 10, 27, 5, 0, 0)) != null, "DST repetition rejected");
        Check(policy.Validate(new(2030, 3, 31, 2, 30, 0), new(2030, 3, 31, 4, 30, 0)) == null, "valid DST crossing accepted");
        Check(policy.Validate(DateTime.SpecifyKind(Future, DateTimeKind.Utc), Future.AddHours(1)) != null, "UTC payload rejected");
        _passed++; Console.WriteLine("PASS Romanian time boundaries and DST");
    }

    /// <summary>Checks allowed states, identity isolation, and repeat decisions.</summary>
    private static async Task Transitions()
    {
        var (listing, first, second) = await Fixture();
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _renter, Ct)).IsSuccessful == false, "non-owner denied");
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "pending approved");
        DateTime? response;
        await using (var db = Context())
        {
            var rows = await db.Bookings.Where(b => b.ListingId == listing).ToListAsync();
            response = rows.Single(b => b.Id == first).OwnerResponseAt;
            Check(response != null && rows.Single(b => b.Id == second).OwnerResponseAt == response, "all response timestamps set");
        }
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "repeat approval succeeds");
        await using (var db = Context()) Check((await BookingService(db).RejectAsync(second, _owner, null, Ct)).IsSuccessful, "repeat rejection succeeds");
        await using (var db = Context())
        {
            var result = await new ApproveBookingHandler(BookingService(db)).Handle(new() { BookingId = second, OwnerId = _owner }, Ct);
            Check(result.IsConflict, "rejected approval is conflict through handler");
        }
        await using (var db = Context()) Check((await BookingService(db).RejectAsync(first, _owner, null, Ct)).IsConflict, "confirmed rejection conflict");
        await using (var db = Context())
        {
            Check((await db.Bookings.FindAsync(first))!.OwnerResponseAt == response, "repeat preserves timestamp");
            await db.Bookings.Where(b => b.Id == second).ExecuteUpdateAsync(u => u.SetProperty(b => b.Status, BookingStatus.Completed));
        }
        await using (var db = Context()) Check((await BookingService(db).RejectAsync(second, _owner, null, Ct)).IsConflict, "completed reject conflict");
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(second, _owner, Ct)).IsConflict, "completed approve conflict");
        await using (var db = Context()) Check(!(await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _renter, Ct)).IsSuccessful, "ownership before idempotency");
    }

    /// <summary>Checks future validation on mutations and repeat approval of historical confirmed records.</summary>
    private static async Task PastPeriods()
    {
        var (listing, first, _) = await Fixture();
        var past = new DateTime(2020, 1, 1, 10, 0, 0);
        await using (var db = Context())
        {
            var result = await BookingService(db).CreateAsync(new() { ListingId = listing, RenterId = _renter, StartDateTime = past, EndDateTime = past.AddHours(1) }, Ct);
            Check(!result.IsSuccessful && !result.IsConflict, "past create validation error");
            await db.Bookings.Where(b => b.Id == first).ExecuteUpdateAsync(u => u.SetProperty(b => b.StartDateTime, past).SetProperty(b => b.EndDateTime, past.AddHours(1)));
        }
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsConflict, "expired Pending conflict");
        await using (var db = Context()) await db.Bookings.Where(b => b.Id == first).ExecuteUpdateAsync(u => u.SetProperty(b => b.Status, BookingStatus.Confirmed));
        await using (var db = Context()) Check((await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "historical confirmed idempotent");
    }

    /// <summary>A request that expires while waiting must fail validation after acquiring its lock.</summary>
    private static async Task TimeAfterLock()
    {
        var (listing, _, _) = await Fixture();
        var clock = new MutableClock();
        await using var blocker = Context();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await new ListingRepository(blocker).LockAsync(listing, Ct);
        var signal = new LockSignal();
        await using var request = Context(signal);
        var service = new BookingService(new BookingRepository(request), new ListingRepository(request), Availability(request),
            new RepositoryTransactionCoordinator(request), new BookingTimePolicy(clock), NullLogger<BookingService>.Instance, new NotificationRepository(request));
        var start = new DateTime(2026, 9, 12, 15, 1, 0);
        var pending = service.CreateAsync(new() { ListingId = listing, RenterId = _renter, StartDateTime = start, EndDateTime = start.AddHours(1) }, Ct);
        await signal.Started.Task.WaitAsync(TimeSpan.FromSeconds(15));
        clock.UtcNow = clock.UtcNow.AddMinutes(2);
        await transaction.CommitAsync();
        var result = await pending;
        Check(!result.IsSuccessful && result.ErrorsSummary.Contains("future"), "time evaluated after lock wait");
    }

    /// <summary>Releases two requests only after both have reached the contended listing lock.</summary>
    private static async Task Race(long listing, Func<ParkShareDbContext, Task> first, Func<ParkShareDbContext, Task> second)
    {
        await using var blocker = Context();
        await using var transaction = await blocker.Database.BeginTransactionAsync();
        await new ListingRepository(blocker).LockAsync(listing, Ct);
        var signal1 = new LockSignal(); var signal2 = new LockSignal();
        await using var db1 = Context(signal1); await using var db2 = Context(signal2);
        var task1 = first(db1); var task2 = second(db2);
        await Task.WhenAll(signal1.Started.Task, signal2.Started.Task).WaitAsync(TimeSpan.FromSeconds(15));
        Check(!task1.IsCompleted && !task2.IsCompleted, "requests wait for held lock");
        await transaction.CommitAsync();
        await Task.WhenAll(task1, task2).WaitAsync(TimeSpan.FromSeconds(30));
    }

    /// <summary>Only one overlapping Pending request may become Confirmed.</summary>
    private static async Task ConcurrentApprovals()
    {
        var (listing, first, second) = await Fixture();
        ServiceResult? a = null, b = null;
        await Race(listing, async db => a = await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct),
            async db => b = await BookingService(db).ApproveAndRejectOverlappingPendingAsync(second, _owner, Ct));
        Check(a!.IsSuccessful != b!.IsSuccessful && (a.IsConflict || b.IsConflict), "one approval and one conflict");
        await using var check = Context();
        Check(await check.Bookings.CountAsync(x => x.ListingId == listing && x.Status == BookingStatus.Confirmed) == 1, "single confirmed record");
        Check(await check.Notifications.CountAsync(n => n.Booking!.ListingId == listing) == 2, "concurrent approvals emit only winning decision events");
    }

    /// <summary>Concurrent opposing decisions cannot overwrite a committed terminal state.</summary>
    private static async Task ConcurrentDecisions()
    {
        var (listing, first, _) = await Fixture();
        ServiceResult? a = null, b = null;
        await Race(listing, async db => a = await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct),
            async db => b = await BookingService(db).RejectAsync(first, _owner, null, Ct));
        Check(a!.IsSuccessful != b!.IsSuccessful && (a.IsConflict || b.IsConflict), "one opposing decision wins");
    }

    /// <summary>A block and approval for the same period cannot both succeed.</summary>
    private static async Task ConcurrentBlock()
    {
        var (listing, first, _) = await Fixture();
        ServiceResult? a = null, b = null;
        await Race(listing, async db => a = await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct),
            async db => b = await Availability(db).CreateAsync(listing, _owner, Future, Future.AddHours(1), true, null, Ct));
        Check(a!.IsSuccessful != b!.IsSuccessful, "only approval or block succeeds");
    }

    /// <summary>Failure after confirming but before rejecting overlaps rolls the entire decision back.</summary>
    private static async Task Rollback()
    {
        var (listing, first, _) = await Fixture();
        await using (var db = Context(new FailOverlapUpdate()))
            Check(!(await BookingService(db).ApproveAndRejectOverlappingPendingAsync(first, _owner, Ct)).IsSuccessful, "injected failure returned");
        await using var check = Context();
        Check(await check.Bookings.Where(b => b.ListingId == listing).AllAsync(b => b.Status == BookingStatus.Pending && b.OwnerResponseAt == null), "all writes rolled back");
    }

    /// <summary>Listing changes and window writes participate in the shared lock convention.</summary>
    private static async Task MutationLocks()
    {
        var (listing, _, _) = await Fixture();
        await Race(listing, async db => Check((await new ListingService(new UserRepository(db), new UserRoleRepository(db), new ListingRepository(db),
                new RepositoryTransactionCoordinator(db), NullLogger<ListingService>.Instance, new AmenityRepository(db), new ListingAmenityRepository(db)).DeactivateAsync(listing, _owner, Ct)).IsSuccessful, "deactivate succeeds"),
            async db => Check((await Availability(db).CreateAsync(listing, _owner, Future.AddDays(1), Future.AddDays(1).AddHours(1), true, null, Ct)).IsSuccessful, "window create succeeds"));
    }

    /// <summary>Creates fresh overlapping requests on an isolated listing.</summary>
    private static async Task<(long Listing, long First, long Second)> Fixture()
    {
        await using var db = Context();
        var listing = new Listing { OwnerId = _owner, Title = "Test space", Description = "Test", PricePerHour = 10, IsAlwaysAvailable = true };
        db.Listings.Add(listing); await db.SaveChangesAsync();
        var first = new Booking { ListingId = listing.Id, RenterId = _renter, StartDateTime = Future, EndDateTime = Future.AddHours(1) };
        var second = new Booking { ListingId = listing.Id, RenterId = _renter, StartDateTime = Future, EndDateTime = Future.AddHours(1) };
        db.Bookings.AddRange(first, second); await db.SaveChangesAsync();
        return (listing.Id, first.Id, second.Id);
    }

    /// <summary>Creates an independent request context, optionally instrumenting SQL commands.</summary>
    private static ParkShareDbContext Context(params IInterceptor[] interceptors)
        => new(new DbContextOptionsBuilder<ParkShareDbContext>().UseSqlServer(_connection).AddInterceptors(interceptors).Options);

    /// <summary>Creates the real availability service and SQL repositories.</summary>
    private static AvailabilityService Availability(ParkShareDbContext db)
        => new(new ListingRepository(db), new AvailabilityWindowRepository(db), new BookingRepository(db), new RepositoryTransactionCoordinator(db), NullLogger<AvailabilityService>.Instance);

    /// <summary>Creates the real booking service and SQL repositories.</summary>
    private static BookingService BookingService(ParkShareDbContext db)
        => new(new BookingRepository(db), new ListingRepository(db), Availability(db), new RepositoryTransactionCoordinator(db), new BookingTimePolicy(Clock), NullLogger<BookingService>.Instance, new NotificationRepository(db));

    /// <summary>Creates a test account.</summary>
    private static User User(string name) => new() { UserName = name, Email = name + "@test.invalid", FirstName = name, LastName = "Test", PasswordHash = "fixture", IsActive = true };

    /// <summary>Runs and reports a regression group.</summary>
    private static async Task Test(string name, Func<Task> run) { await run(); _passed++; Console.WriteLine("PASS " + name); }

    /// <summary>Fails the executable on a violated invariant.</summary>
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    /// <summary>Provides a deterministic instant for booking tests.</summary>
    private sealed class FixedClock : TimeProvider
    {
        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
    }

    /// <summary>Allows deterministic clock advancement while a database request is blocked.</summary>
    private sealed class MutableClock : TimeProvider
    {
        /// <summary>Current test instant.</summary>
        public DateTimeOffset UtcNow { get; set; } = new(2026, 9, 12, 12, 0, 0, TimeSpan.Zero);
        /// <inheritdoc/>
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    /// <summary>Signals that a request has attempted its SQL listing lock.</summary>
    private sealed class LockSignal : DbCommandInterceptor
    {
        /// <summary>Completes when the locking query reaches SQL execution.</summary>
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK")) Started.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }

    /// <summary>Injects a failure into the bulk rejection after the first booking write.</summary>
    private sealed class FailOverlapUpdate : DbCommandInterceptor
    {
        /// <inheritdoc/>
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDATE") && command.CommandText.Contains("Bookings"))
                throw new InvalidOperationException("Injected overlap update failure.");
            return ValueTask.FromResult(result);
        }
    }
}
