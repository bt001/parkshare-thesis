using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Availability
{
    /// <summary>
    /// API request for updating an availability window.
    /// </summary>
    public class UpdateAvailabilityWindowRequest : IApiRequest
    {
        /// <summary>
        /// Availability window ID.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Owner ID, set by the API from the authenticated user.
        /// </summary>
        public long OwnerId { get; set; }
        /// <summary>
        /// Updated start time.
        /// </summary>
        public DateTime StartDateTime { get; set; }
        /// <summary>
        /// Updated end time.
        /// </summary>
        public DateTime EndDateTime { get; set; }
        /// <summary>
        /// Updated blocked flag.
        /// </summary>
        public bool IsBlocked { get; set; }
        /// <summary>
        /// Updated note.
        /// </summary>
        public string? Note { get; set; }
    }
}
