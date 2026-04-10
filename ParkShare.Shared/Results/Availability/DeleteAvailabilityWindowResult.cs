namespace ParkShare.Shared.Results.Availability
{
    /// <summary>
    /// API result for deleting an availability window.
    /// </summary>
    public class DeleteAvailabilityWindowResult : IApiResult
    {
        /// <summary>
        /// Deleted availability window ID.
        /// </summary>
        public long AvailabilityWindowId { get; set; }
    }
}
