using Microsoft.EntityFrameworkCore;
using ParkShare.Application.Queries;
using ParkShare.Application.Queries.Validators;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
using ParkShare.Infrastructure.Repositories;

internal static partial class Program
{
    /// <summary>Verifies period unions and geographic filtering in real SQL before pagination.</summary>
    private static async Task SearchAvailability()
    {
        var prefix = "Search-" + Guid.NewGuid().ToString("N");
        await using var db = Context();
        var rows = Enumerable.Range(0, 9).Select(i => new Listing
        {
            OwnerId = _owner, Title = prefix + i, Description = "Search fixture", Latitude = 44.43m,
            Longitude = 26.10m, PricePerHour = 10, IsAlwaysAvailable = i >= 4
        }).ToArray();
        db.Listings.AddRange(rows); await db.SaveChangesAsync();
        void Window(int index, double start, double end, bool blocked = false) => db.AvailabilityWindows.Add(new()
        { ListingId = rows[index].Id, StartDateTime = Future.AddHours(start), EndDateTime = Future.AddHours(end), IsBlocked = blocked });
        Window(0, -1, 1); Window(0, 1, 2); // Touching union.
        Window(1, -1, 0.5); Window(1, 1, 2); // Gap.
        Window(2, -1, 3); Window(2, 0.2, 0.5); // Nested window.
        Window(3, 0, 2); Window(3, 1, 1.5, true);
        Window(4, -1, 0, true); Window(4, 2, 3, true); // Exact boundaries.
        db.Bookings.AddRange(new Booking { ListingId = rows[5].Id, RenterId = _renter, StartDateTime = Future, EndDateTime = Future.AddHours(2), Status = BookingStatus.Confirmed },
            new Booking { ListingId = rows[6].Id, RenterId = _renter, StartDateTime = Future, EndDateTime = Future.AddHours(2) });
        rows[7].IsActive = false; rows[8].Longitude = 27;
        await db.SaveChangesAsync();
        await using var fresh = Context();
        var repository = new ListingRepository(fresh);
        var result = await repository.SearchAsync(44.43, 26.10, 5, null, null, true, null, prefix, null, 1, 2, Ct, Future, Future.AddHours(2));
        Check(result.TotalCount == 4 && result.Items.Count == 2, "availability and radius applied before counts/paging");
        var second = await repository.SearchAsync(44.43, 26.10, 5, null, null, true, null, prefix, null, 2, 2, Ct, Future, Future.AddHours(2));
        Check(result.Items.Concat(second.Items).Select(x => x.Id).Order().SequenceEqual(new[] { rows[0].Id, rows[2].Id, rows[4].Id, rows[6].Id }.Order()), "union, gap, nested, blocked, confirmed, pending and inactive semantics");
        Check((await BookingService(fresh).CreateAsync(new ParkShare.Application.Commands.CreateBookingCommand { ListingId = rows[0].Id, RenterId = _renter, StartDateTime = Future, EndDateTime = Future.AddHours(2) }, Ct)).IsSuccessful, "searched touching union remains bookable");
        Window(0, 0, 2, true); await db.SaveChangesAsync();
        Check(!(await BookingService(fresh).CreateAsync(new ParkShare.Application.Commands.CreateBookingCommand { ListingId = rows[0].Id, RenterId = _renter, StartDateTime = Future, EndDateTime = Future.AddHours(2) }, Ct)).IsSuccessful, "fresh locked booking check rejects intervening block");
        var validator = new SearchListingsQueryValidator();
        foreach (var pair in new (DateTime?, DateTime?)[] { (Future, null), (null, Future), (Future, Future), (new(2020, 1, 1), Future), (new(2030, 3, 31, 3, 30, 0), new(2030, 3, 31, 5, 0, 0)), (new(2030, 10, 27, 3, 30, 0), new(2030, 10, 27, 5, 0, 0)), (DateTime.SpecifyKind(Future, DateTimeKind.Utc), Future.AddHours(2)) })
            Check(!validator.Validate(new SearchListingsQuery { Page = 1, PageSize = 12, StartDateTime = pair.Item1, EndDateTime = pair.Item2 }).IsValid, "invalid period rejected");
    }
}
