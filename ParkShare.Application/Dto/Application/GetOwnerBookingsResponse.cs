using ParkShare.Common.Enums;

namespace ParkShare.Application.Dto.Application
{
    public class GetOwnerBookingsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<OwnerBookingItem> Items { get; set; } = new List<OwnerBookingItem>();
    }

    public class OwnerBookingItem
    {
        public long BookingId { get; set; }
        public long ListingId { get; set; }
        public string ListingTitle { get; set; } = string.Empty;
        public long RenterId { get; set; }
        public DateTime StartUtc { get; set; }
        /// <summary>Whether the server currently permits owner completion.</summary>
        public bool CanComplete { get; set; }
        public DateTime EndUtc { get; set; }
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
