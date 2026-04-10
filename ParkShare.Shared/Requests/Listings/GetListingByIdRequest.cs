using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class GetListingByIdRequest : IApiRequest
    {
        public long Id { get; set; }
    }

}
