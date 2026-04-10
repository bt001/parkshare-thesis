namespace ParkShare.Shared.Results.Availability
{
    /// <summary>
    /// API result for availability mutations.
    /// </summary>
    public class AvailabilityWindowMutationResult : IApiResult
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
        /// Listing title.
        /// </summary>
        public string ListingTitle { get; set; } = string.Empty;
        /// <summary>
        /// Window start time.
        /// </summary>
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// Window end time.
        /// </summary>
        public DateTime EndDateTime { get; set; }
        /// <summary>
        /// Whether the window blocks availability.
        /// </summary>
        public bool IsBlocked { get; set; }
        /// <summary>
        /// Optional note.
        /// </summary>
        public string? Note { get; set; }
        /// <summary>
        /// Creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
