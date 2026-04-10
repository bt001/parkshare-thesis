using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Availability
{
    /// <summary>
    /// API request for listing availability.
    /// </summary>
    public class GetListingAvailabilityRequest : IApiRequest
    {
        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; set; }
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
