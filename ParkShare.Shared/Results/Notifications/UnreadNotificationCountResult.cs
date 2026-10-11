namespace ParkShare.Shared.Results.Notifications
{
    /// <summary>UnreadNotificationCountResult data contract.</summary>
    public class UnreadNotificationCountResult : IApiResult
    {
        /// <summary>Total unread notifications.</summary>
        public int Count { get; set; }
    }
}
