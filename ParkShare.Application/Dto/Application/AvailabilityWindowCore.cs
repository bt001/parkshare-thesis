namespace ParkShare.Application.Dto.Application
{
    /// <summary>
    /// Core ownership and timing data for an availability window.
    /// </summary>
    public class AvailabilityWindowCore
    {
        /// <summary>
        /// Availability window ID.
        /// </summary>
        public long AvailabilityWindowId { get; set; }
        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; set; }
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; set; }
        /// <summary>
        /// Whether the window blocks availability.
        /// </summary>
        public bool IsBlocked { get; set; }
        /// <summary>
        /// Window start time.
        /// </summary>
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// Window end time.
        /// </summary>
        public DateTime EndDateTime { get; set; }
    }
}
