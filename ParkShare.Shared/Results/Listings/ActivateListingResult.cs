namespace ParkShare.Shared.Results.Listings
{
    public class ActivateListingResult : IApiResult
    {
        public long ListingId { get; set; }
        public bool IsActive { get; set; }
    }
}
