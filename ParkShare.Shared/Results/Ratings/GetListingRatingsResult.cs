namespace ParkShare.Shared.Results.Ratings
{
    /// <summary>Listing review page and aggregate across all reviews.</summary>
    public class GetListingRatingsResult : IApiResult
    {
        /// <summary>One-based page.</summary>
        public int Page { get; set; }
        /// <summary>Maximum page size.</summary>
        public int PageSize { get; set; }
        /// <summary>Total reviews.</summary>
        public int TotalCount { get; set; }
        /// <summary>Average stars, or null when no reviews exist.</summary>
        public double? AverageStars { get; set; }
        /// <summary>Reviews ordered newest first.</summary>
        public List<RatingResult> Items { get; set; } = new();
    }
}
