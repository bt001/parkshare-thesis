using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class CreateBookingRequest : IApiRequest
    {
        public long ListingId { get; set; }
        public long RenterId { get; set; }      // until real auth
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
    }
}
