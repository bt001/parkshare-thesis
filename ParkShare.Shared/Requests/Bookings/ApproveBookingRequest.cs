using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class ApproveBookingRequest : IApiRequest
    {
        public long BookingId { get; set; }
        public long OwnerId { get; set; }
    }
}
