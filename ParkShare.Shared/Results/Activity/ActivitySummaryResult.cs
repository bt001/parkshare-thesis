using ParkShare.Shared.Results;
namespace ParkShare.Shared.Results.Activity;
/// <summary>ActivitySummaryResult API contract.</summary>
public class ActivitySummaryResult : IApiResult
{
    /// <summary>Unread inbox events.</summary>
    public int UnreadNotificationCount { get; set; }
    /// <summary>Distinct owned bookings needing attention.</summary>
    public int OwnerAttentionCount { get; set; }
    /// <summary>Distinct rented bookings needing attention.</summary>
    public int RenterAttentionCount { get; set; }
}
