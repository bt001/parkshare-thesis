namespace ParkShare.Shared.Results.Notifications
{
    /// <summary>GetNotificationsResult data contract.</summary>
    public class GetNotificationsResult : IApiResult
    {
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum items per page.</summary>
        public int PageSize { get; set; } = 20;
        /// <summary>Total matching notifications.</summary>
        public int TotalCount { get; set; }
        /// <summary>Page of recipient-owned notifications.</summary>
        public List<NotificationItemResult> Items { get; set; } = new();
    }
}
