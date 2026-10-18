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
    /// <summary>Checks projected summaries, combined filters, stable ordering and application estimates.</summary>
    private static async Task SearchQuality()
    {
        await using var db = Context();
        var prefix = "Quality-" + Guid.NewGuid().ToString("N");
        var amenities = await db.Amenities.OrderBy(a => a.Id).Take(2).Select(a => a.Id).ToArrayAsync();
        var rows = Enumerable.Range(0, 4).Select(i => new Listing { OwnerId = _owner,
            Title = prefix + i, Description = "Quality fixture", PricePerHour = i == 0 ? 10.01m : 20,
            Latitude = 44.43m, Longitude = 26.1m, CreatedAt = Future, IsAlwaysAvailable = true }).ToArray();
        db.Listings.AddRange(rows); await db.SaveChangesAsync();
        foreach (var row in rows.Take(3))
            db.ListingAmenities.Add(new() { ListingId = row.Id, AmenityId = amenities[0] });
        foreach (var row in rows.Take(2))
            db.ListingAmenities.Add(new() { ListingId = row.Id, AmenityId = amenities[1] });
        foreach (var (index, stars) in new[] { (0, 4), (0, 5), (1, 5), (2, 4), (2, 4), (2, 5) })
        {
            var booking = new Booking { ListingId = rows[index].Id, RenterId = _renter,
                StartDateTime = Future.AddDays(-1), EndDateTime = Future.AddDays(-1).AddHours(1), Status = BookingStatus.Completed };
            db.Bookings.Add(booking); await db.SaveChangesAsync();
            db.Ratings.Add(new() { ListingId = rows[index].Id, BookingId = booking.Id, RenterId = _renter, Stars = stars });
        }
        var cover = new ListingPhoto { ListingId = rows[0].Id, UploadedAt = Future, Url = "private-key" };
        db.ListingPhotos.Add(cover); await db.SaveChangesAsync();
        db.ListingPhotos.Add(new() { ListingId = rows[0].Id, UploadedAt = Future, Url = "second-private-key" });
        await db.SaveChangesAsync();
        await using var fresh = Context();
        var repository = new ListingRepository(fresh);
        async Task<ParkShare.Application.Dto.Infrastructure.PagedResult<ParkShare.Application.Dto.Infrastructure.ListingSearchRow>> Search(
            string sort = "newest", long[]? ids = null, double? rating = null, int page = 1, int size = 20)
            => await repository.SearchAsync(44.43, 26.1, 5, 0, 20, true, true, prefix, null, page, size, Ct,
                Future, Future.AddHours(1.5), ids, rating, sort);
        var result = await Search("priceAsc", amenities.Concat(amenities).ToArray(), 4.5, size: 1);
        Check(result.TotalCount == 2 && result.Items.Single().Id == rows[0].Id, "combined filters and duplicate amenities before paging");
        Check((await Search("priceAsc", amenities, 4.5, page: 2, size: 1)).Items.Single().Id == rows[1].Id, "stable filtered second page");
        var first = result.Items.Single();
        Check(first.AverageRating == 4.5 && first.RatingCount == 2 && first.CoverPhotoId == cover.Id, "aggregate and earliest equal-time cover");
        Check((await Search(ids: new[] { long.MaxValue })).TotalCount == 0, "unknown amenity has no matches");
        Check((await Search(rating: 4.34)).TotalCount == 2, "unrounded threshold excludes 4.333 and unrated");
        Check((await Search("ratingDesc")).Items.Select(x => x.Id).SequenceEqual(new[] { rows[1].Id, rows[0].Id, rows[2].Id, rows[3].Id }), "rating ordering and unrated last");
        Check((await Search("priceDesc")).Items.Select(x => x.Id).SequenceEqual(new[] { rows[3].Id, rows[2].Id, rows[1].Id, rows[0].Id }), "stable price ties");
        Check((await Search()).Items.First().Id == rows[3].Id, "default newest ties");
        var priced = await Listings(fresh).SearchAsync(null, null, null, null, null, true, null, rows[0].Title, null, 1, 20, Ct, Future, Future.AddMinutes(90));
        Check(priced.IsSuccessful && priced.Payload.Items.Single().EstimatedPeriodCost == 15.02m, "fractional estimate rounds midpoint away from zero");
        Check(priced.Payload.Items.Single().CoverPhotoUrl == $"api/listing/{rows[0].Id}/photos/{cover.Id}/content", "public photo endpoint hides private key");
        var noPeriod = await Listings(fresh).SearchAsync(null, null, null, null, null, true, null, rows[3].Title, null, 1, 20, Ct);
        Check(noPeriod.Payload.Items.Single().EstimatedPeriodCost is null && noPeriod.Payload.Items.Single().AverageRating is null
            && noPeriod.Payload.Items.Single().CoverPhotoUrl is null, "missing period, rating and photo fallbacks");
        var validator = new SearchListingsQueryValidator();
        foreach (var query in new[] { new SearchListingsQuery { Page = 1, PageSize = 20, AmenityIds = new long[] { 0 } },
            new SearchListingsQuery { Page = 1, PageSize = 20, MinRating = 5.1 }, new SearchListingsQuery { Page = 1, PageSize = 20, MinRating = double.NaN },
            new SearchListingsQuery { Page = 1, PageSize = 20, SortBy = "invalid" } })
            Check(!validator.Validate(query).IsValid, "invalid secondary filters rejected");
    }
}
