using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Ratings
{
    /// <summary>GetListingRatings API input.</summary>
    public class GetListingRatingsRequest : IApiRequest
    {
        /// <summary>Listing identity from the route.</summary>
        public long ListingId { get; set; }
        /// <summary>One-based page.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum page size.</summary>
        public int PageSize { get; set; } = 20;
    }
}
