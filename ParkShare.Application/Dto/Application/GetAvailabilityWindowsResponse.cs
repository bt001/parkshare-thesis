namespace ParkShare.Application.Dto.Application
{
    /// <summary>
    /// Availability query result.
    /// </summary>
    public class GetAvailabilityWindowsResponse
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
        public List<AvailabilityWindowResponse> Items { get; set; } = new();
    }
}
