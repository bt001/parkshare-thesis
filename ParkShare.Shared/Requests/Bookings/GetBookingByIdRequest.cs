using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class GetBookingByIdRequest : IApiRequest
    {
        public long Id { get; set; }
        /// <summary>
        /// Authenticated user requesting the booking.
        /// </summary>
        public long RequesterId { get; set; }
    }
}
