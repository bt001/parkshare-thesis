namespace ParkShare.Shared.Results.Notifications
{
    /// <summary>MarkNotificationReadResult data contract.</summary>
    public class MarkNotificationReadResult : IApiResult
    {
        /// <summary>Persisted read state.</summary>
        public bool IsRead { get; set; }
    }
}
