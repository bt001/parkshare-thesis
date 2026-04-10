using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class SearchListingsRequest : IApiRequest
    {
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public double? RadiusKm { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? OnlyActive { get; set; }
        public bool? OnlyAlwaysAvailable { get; set; }
        public string? TitleContains { get; set; }      // NEW
        public string? AddressContains { get; set; }    // NEW
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

}
