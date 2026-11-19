using ParkShare.Common.Enums;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces.Repositories;

/// <summary>Reads participant-scoped activity without loading full histories.</summary>
public interface IActivityRepository
{
    /// <summary>Counts distinct bookings needing the authenticated user's attention.</summary>
    Task<ActivitySummaryResponse> GetSummaryAsync(long userId, CancellationToken ct);
    /// <summary>Reads activity only for the supplied participant booking page.</summary>
    Task<Dictionary<long, BookingActivityResponse>> GetBookingsAsync(long userId, IReadOnlyDictionary<long, BookingStatus> bookingStatuses, CancellationToken ct);
}
