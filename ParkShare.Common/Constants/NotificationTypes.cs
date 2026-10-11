namespace ParkShare.Common.Constants
{
    /// <summary>Stable persisted booking notification types.</summary>
    public static class NotificationTypes
    {
        /// <summary>A renter requested a booking.</summary>
        public const string BookingRequested = "booking-requested";
        /// <summary>The owner approved a booking.</summary>
        public const string BookingApproved = "booking-approved";
        /// <summary>The owner rejected a booking.</summary>
        public const string BookingRejected = "booking-rejected";
        /// <summary>Approval of another booking rejected this request.</summary>
        public const string BookingOverlapRejected = "booking-overlap-rejected";
    }
}
