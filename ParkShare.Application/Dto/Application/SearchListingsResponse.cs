namespace ParkShare.Application.Dto.Application
{
    public class SearchListingsResponse
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<SearchListingItemDto> Items { get; set; } = new List<SearchListingItemDto>();
    }

    public class SearchListingItemDto
    {
        /// <summary>Public cover image endpoint, when a photo exists.</summary>
        public string? CoverPhotoUrl { get; set; }
        /// <summary>Unrounded average, or null without reviews.</summary>
        public double? AverageRating { get; set; }
        /// <summary>Number of saved reviews.</summary>
        public int RatingCount { get; set; }
        /// <summary>Estimated RON cost for the requested wall-clock period.</summary>
        public decimal? EstimatedPeriodCost { get; set; }
        public long Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Address { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
        public bool IsAlwaysAvailable { get; set; }
        public DateTime CreatedAt { get; set; }
    }
 
}
