namespace ParkShare.Shared.Results.Listings
{
    public class DeactivateListingResult : IApiResult
    {
        public long ListingId { get; set; }
        public bool IsActive { get; set; }
    }

}
