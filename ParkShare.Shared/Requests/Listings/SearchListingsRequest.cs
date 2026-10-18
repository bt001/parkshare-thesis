using ParkShare.Shared.Results;

namespace ParkShare.Shared.Requests.Listings
{
    public class SearchListingsRequest : IApiRequest
    {
        /// <summary>Requested arrival in Romanian local time, paired with departure.</summary>
        public DateTime? StartDateTime { get; set; }
        /// <summary>Requested departure in Romanian local time.</summary>
        public DateTime? EndDateTime { get; set; }
        /// <summary>All selected amenity identifiers must match.</summary>
        public long[]? AmenityIds { get; set; }
        /// <summary>Inclusive unrounded review average threshold.</summary>
        public double? MinRating { get; set; }
        /// <summary>Ordering: newest, priceAsc, priceDesc or ratingDesc.</summary>
        public string? SortBy { get; set; }
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
