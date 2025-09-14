using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class RejectBookingRequest : IApiRequest
    {
        public long BookingId { get; set; }
        public long OwnerId { get; set; }
        public string? Reason { get; set; }
    }
}
