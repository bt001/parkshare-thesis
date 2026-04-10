using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Availability
{
    /// <summary>
    /// API request for deleting an availability window.
    /// </summary>
    public class DeleteAvailabilityWindowRequest : IApiRequest
    {
        /// <summary>
        /// Availability window ID.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; set; }
    }
}
