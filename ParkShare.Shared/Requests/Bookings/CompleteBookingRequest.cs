using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Bookings
{
    /// <summary>Owner request to complete an ended booking.</summary>
    public class CompleteBookingRequest : IApiRequest
    {
        /// <summary>Booking to complete.</summary>
        public long BookingId { get; set; }
        /// <summary>Owner identity supplied by the server.</summary>
        public long OwnerId { get; set; }
    }
}
