using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class ActivateListingRequest : IApiRequest
    {
        public long Id { get; set; }
        /// <summary>
        /// Owner ID, set by the API from the authenticated user.
        /// </summary>
        public long OwnerId { get; set; }
    }
}
