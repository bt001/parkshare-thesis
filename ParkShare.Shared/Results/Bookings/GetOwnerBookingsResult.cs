using ParkShare.Common.Enums;

namespace ParkShare.Shared.Results.Bookings
{
    public class GetOwnerBookingsResult : IApiResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<OwnerBookingItemResult> Items { get; set; } = new List<OwnerBookingItemResult>();
    }

    public class OwnerBookingItemResult
    {
        public long BookingId { get; set; }
        public long ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public long RenterId { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public DateTime CreatedAt { get; set; }
    }
}
