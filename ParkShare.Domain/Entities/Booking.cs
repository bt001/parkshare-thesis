namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Booking for a listing.
    /// Includes status and period.
    /// </summary>
    public class Booking
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

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
        /// Booking start datetime.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Booking end datetime.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Request status (Pending, Approved, etc.).
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Booking creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Owner response timestamp (nullable).
        /// </summary>
        public DateTime? OwnerResponseAt { get; set; }

        /// <summary>
        /// Payment status (placeholder).
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
        public Rating? Rating { get; set; }
    }
}
