using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class RejectBookingRequest : IApiRequest
    {
        public long BookingId { get; set; }
        /// <summary>
        /// Owner ID, set by the API from the authenticated user.
        /// </summary>
        public long OwnerId { get; set; }
        public string? Reason { get; set; }
    }
}
