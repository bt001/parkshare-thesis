using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Ratings
{
    /// <summary>SaveBookingRating API input.</summary>
    public class SaveBookingRatingRequest : IApiRequest
    {
        /// <summary>Booking identity from the route.</summary>
        public long BookingId { get; set; }
        /// <summary>Renter identity supplied by the server.</summary>
        public long RenterId { get; set; }
        /// <summary>Stars from one to five.</summary>
        public int Stars { get; set; }
        /// <summary>Optional review text.</summary>
        public string? Comment { get; set; }
    }
}
