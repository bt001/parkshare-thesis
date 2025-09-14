using ParkShare.Common.Enums;

namespace ParkShare.Shared.Results.Bookings
{
    public class BookingResult : IApiResult
    {
        public long BookingId { get; set; }
        public long ListingId { get; set; }
        public long RenterId { get; set; }
        public long OwnerId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public BookingStatus Status { get; set; }
        public string StatusText => Status.ToString();
        public DateTime CreatedAt { get; set; }
    }

}
