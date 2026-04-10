using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class ActivateListingRequest : IApiRequest
    {
        public long Id { get; set; }
        public long OwnerId { get; set; }
    }
}
