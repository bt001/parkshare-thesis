namespace ParkShare.Domain.Entities
{
    /// <summary>
    /// Listing availability window.
    /// Defines open or blocked periods.
    /// </summary>
    public class AvailabilityWindow
    {
        /// <summary>
        /// Primary key.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; set; }
        public Listing Listing { get; set; } = null!;

        /// <summary>
        /// Availability start datetime.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Availability end datetime.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// True if window is a block (unavailable).
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// Optional note for the block.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Creation date.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
