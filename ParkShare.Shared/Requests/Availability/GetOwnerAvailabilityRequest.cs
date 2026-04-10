using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Availability
{
    /// <summary>
    /// API request for owner availability.
    /// </summary>
    public class GetOwnerAvailabilityRequest : IApiRequest
    {
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; set; }
        /// <summary>
        /// Optional listing filter.
        /// </summary>
        public long? ListingId { get; set; }
        /// <summary>
        /// Optional lower bound.
        /// </summary>
        public DateTime? StartDateTime { get; set; }
        /// <summary>
        /// Optional upper bound.
        /// </summary>
        public DateTime? EndDateTime { get; set; }
    }
}
