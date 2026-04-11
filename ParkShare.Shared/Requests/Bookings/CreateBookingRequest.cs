using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Bookings
{
    public class CreateBookingRequest : IApiRequest
    {
        public long ListingId { get; set; }
        /// <summary>
        /// Renter ID, set by the API from the authenticated user.
        /// </summary>
        public long RenterId { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
    }
}
