namespace ParkShare.Application.Dto.Application;

/// <summary>Account-scoped counts across all booking pages.</summary>
public class ActivitySummaryResponse
{
    /// <summary>Unread inbox notifications, excluding chat messages.</summary>
    public int UnreadNotificationCount { get; set; }
    /// <summary>Distinct owned bookings pending a decision or containing unread incoming chat.</summary>
    public int OwnerAttentionCount { get; set; }
    /// <summary>Distinct rented bookings containing unseen updates or incoming chat.</summary>
    public int RenterAttentionCount { get; set; }
}

/// <summary>Unread activity represented by one participant booking row.</summary>
public class BookingActivityResponse
{
    /// <summary>Incoming messages not yet acknowledged.</summary>
    public int UnreadMessageCount { get; set; }
    /// <summary>Exact recipient notification identities represented by the row.</summary>
    public List<long> UnreadNotificationIds { get; set; } = [];
}

/// <summary>Number of incoming messages newly acknowledged.</summary>
public class ReadMessagesResponse
{
    /// <summary>Rows changed by this acknowledgement.</summary>
    public int UpdatedCount { get; set; }
}
