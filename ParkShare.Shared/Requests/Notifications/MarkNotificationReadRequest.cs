using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Notifications
{
    /// <summary>MarkNotificationReadRequest data contract.</summary>
    public class MarkNotificationReadRequest : IApiRequest
    {
        /// <summary>Recipient identity supplied by the authenticated API.</summary>
        public long UserId { get; set; }
        /// <summary>Notification identity.</summary>
        public long Id { get; set; }
    }
}
