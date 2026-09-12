using ParkShare.Common.Enums;

namespace ParkShare.Shared.Results.Bookings
{
    /// <summary>Paged renter booking history.</summary>
    public class GetRenterBookingsResult : IApiResult
    {
        /// <summary>One-based page number.</summary>
        public int Page { get; set; }
        /// <summary>Maximum number of bookings per page.</summary>
        public int PageSize { get; set; }
        /// <summary>Total matching bookings.</summary>
        public int TotalCount { get; set; }
        /// <summary>Bookings in this page.</summary>
        public List<RenterBookingItemResult> Items { get; set; } = new List<RenterBookingItemResult>();
    }

    /// <summary>A booking with current listing details.</summary>
    public class RenterBookingItemResult
    {
        /// <summary>Booking identifier.</summary>
        public long BookingId { get; set; }
        /// <summary>Listing identifier.</summary>
        public long ListingId { get; set; }
        /// <summary>Current listing title.</summary>
        public string ListingTitle { get; set; } = string.Empty;
        /// <summary>Current listing address.</summary>
        public string ListingAddress { get; set; } = string.Empty;
        /// <summary>Booked start time, preserving the existing booking time semantics.</summary>
        public DateTime StartDateTime { get; set; }
        /// <summary>Booked end time, preserving the existing booking time semantics.</summary>
        public DateTime EndDateTime { get; set; }
        /// <summary>Stored booking status.</summary>
        public BookingStatus Status { get; set; }
        /// <summary>Request creation timestamp.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>Readable booking status.</summary>
        public string StatusText => Status.ToString();
    }
}
