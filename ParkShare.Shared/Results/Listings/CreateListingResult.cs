namespace ParkShare.Shared.Results.Listings
{
    public class CreateListingResult : IApiResult
    {
        public long ListingId { get; set; }
        public bool IsActive { get; set; } = true;
    }

}
