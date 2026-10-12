using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
using ParkShare.Common.Enums;
using ParkShare.Domain.Entities;
namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class RatingService : IRatingService
    {
        private readonly IBookingRepository _bookings;
        private readonly IListingRepository _listings;
        private readonly IRatingRepository _ratings;
        private readonly IRepositoryTransactionCoordinator _transactions;
        private readonly TimeProvider _clock;
        private readonly ILogger<RatingService> _logger;
        /// <summary>Creates the review service.</summary>
        public RatingService(IBookingRepository bookings, IListingRepository listings, IRatingRepository ratings,
            IRepositoryTransactionCoordinator transactions, TimeProvider clock, ILogger<RatingService> logger)
        { _bookings = bookings; _listings = listings; _ratings = ratings; _transactions = transactions; _clock = clock; _logger = logger; }

        /// <inheritdoc/>
        public Task<ServiceResult<RatingResponse>> SaveAsync(long bookingId, long renterId, int stars, string? comment, CancellationToken ct)
            => ExecuteAsync(async () =>
            {
                var text = comment?.Trim() ?? string.Empty;
                if (stars < 1 || stars > 5 || text.Length > 1000)
                    return ServiceResult<RatingResponse>.WithError("Choose 1 to 5 stars and a comment of at most 1000 characters.");
                var listingId = await _bookings.GetListingIdAsync(bookingId, ct);
                if (!listingId.HasValue) return ServiceResult<RatingResponse>.WithError("Booking not found.");
                await using var transaction = await _transactions.BeginTransactionAsync(ct);
                await _listings.LockAsync(listingId.Value, ct);
                var booking = await _bookings.GetByIdWithListingAsync(bookingId, ct);
                if (booking is null || renterId <= 0 || booking.RenterId != renterId)
                    return ServiceResult<RatingResponse>.WithError("Booking not found.");
                if (booking.Status != BookingStatus.Completed)
                    return ServiceResult<RatingResponse>.Conflict("Only completed bookings can be rated.");
                var rating = await _ratings.SaveAsync(new Rating
                {
                    BookingId = booking.Id, ListingId = booking.ListingId, RenterId = booking.RenterId,
                    Stars = stars, Comment = text, CreatedAt = _clock.GetUtcNow().UtcDateTime
                }, ct);
                await transaction.CommitAsync(ct);
                return ServiceResult<RatingResponse>.Success(ToResponse(rating));
            }, ct);

        /// <inheritdoc/>
        public Task<ServiceResult<GetListingRatingsResponse>> GetAsync(long listingId, int page, int pageSize, CancellationToken ct)
            => ExecuteAsync(async () =>
            {
                if (page < 1 || pageSize < 1 || pageSize > 100)
                    return ServiceResult<GetListingRatingsResponse>.WithError("Invalid review page.");
                if (await _listings.GetByIdAsNoTrackingAsync(listingId, ct) is null)
                    return ServiceResult<GetListingRatingsResponse>.WithError("Listing not found.");
                var result = await _ratings.GetAsync(listingId, page, pageSize, ct);
                return ServiceResult<GetListingRatingsResponse>.Success(new()
                {
                    Page = page, PageSize = pageSize, TotalCount = result.TotalCount,
                    AverageStars = await _ratings.AverageAsync(listingId, ct),
                    Items = result.Items.Select(ToResponse).ToList()
                });
            }, ct);

        /// <summary>Maps a saved review without exposing participant identities.</summary>
        public static RatingResponse ToResponse(Rating rating) => new()
        {
            Id = rating.Id, Stars = rating.Stars, Comment = rating.Comment,
            CreatedAt = DateTime.SpecifyKind(rating.CreatedAt, DateTimeKind.Utc)
        };
        /// <summary>Reports safe errors and propagates caller cancellation.</summary>
        private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> action, CancellationToken ct) where T : notnull
        {
            try { return await action(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rating operation failed.");
                return ServiceResult<T>.WithError("Reviews could not be loaded or saved. Please try again.");
            }
        }
    }
}
