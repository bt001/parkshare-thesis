using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Notifications
{
    /// <summary>GetNotificationsRequest data contract.</summary>
    public class GetNotificationsRequest : IApiRequest
    {
        /// <summary>Recipient identity supplied by the authenticated API.</summary>
        public long UserId { get; set; }
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum items per page.</summary>
        public int PageSize { get; set; } = 20;
        /// <summary>Whether to return only unread events.</summary>
        public bool UnreadOnly { get; set; }
    }
}
