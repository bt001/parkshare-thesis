using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class GetOwnerListingsRequest : IApiRequest
    {
        public long OwnerId { get; set; }
    }
}
