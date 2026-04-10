namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Rating left by renter.
    /// One per booking.
    /// </summary>
    public class Rating
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Booking request ID (unique).
        /// </summary>
        public long BookingId { get; set; }

        /// <summary>
        /// Parent booking request.
        /// </summary>
        public Booking Booking { get; set; } = default!;

        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; set; }

        /// <summary>
        /// Parent listing.
        /// </summary>
        public Listing Listing { get; set; } = default!;

        /// <summary>
        /// Renter user ID.
        /// </summary>
        public long RenterId { get; set; }

        /// <summary>
        /// Renter user.
        /// </summary>
        public User Renter { get; set; } = default!;

        /// <summary>
        /// Star rating (1-5).
        /// </summary>
        public int Stars { get; set; }

        /// <summary>
        /// Rating comment.
        /// </summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>
        /// Rating creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
