using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Availability
{
    /// <summary>
    /// API request for creating an availability window.
    /// </summary>
    public class CreateAvailabilityWindowRequest : IApiRequest
    {
        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; set; }
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; set; }
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
    }
}
