namespace ParkShare.Shared.Results.Availability
{
    /// <summary>
    /// API result for availability queries.
    /// </summary>
    public class GetAvailabilityWindowsResult : IApiResult
    {
        /// <summary>
        /// Listing filter when present.
        /// </summary>
        public long? ListingId { get; set; }
        /// <summary>
        /// Total returned items.
        /// </summary>
        public int TotalCount { get; set; }
        /// <summary>
        /// Returned availability windows.
        /// </summary>
        public List<AvailabilityWindowResult> Items { get; set; } = new();
    }
}
