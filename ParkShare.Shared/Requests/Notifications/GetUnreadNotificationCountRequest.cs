using ParkShare.Shared.Results;
namespace ParkShare.Shared.Requests.Notifications
{
    /// <summary>GetUnreadNotificationCountRequest data contract.</summary>
    public class GetUnreadNotificationCountRequest : IApiRequest
    {
        /// <summary>Recipient identity supplied by the authenticated API.</summary>
        public long UserId { get; set; }
    }
}
