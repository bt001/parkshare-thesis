using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
namespace ParkShare.Application.Interfaces
{
    /// <summary>Provides recipient-only inbox operations.</summary>
    public interface INotificationService
    {
        /// <summary>Loads a recipient's paginated inbox.</summary>
        Task<ServiceResult<GetNotificationsResponse>> GetAsync(long userId, bool unreadOnly, int page, int pageSize, CancellationToken ct);
        /// <summary>Counts unread notifications for a recipient.</summary>
        Task<ServiceResult<UnreadNotificationCountResponse>> CountUnreadAsync(long userId, CancellationToken ct);
        /// <summary>Marks a recipient-owned notification read without disclosing other recipients' records.</summary>
        Task<ServiceResult<MarkNotificationReadResponse>> MarkReadAsync(long userId, long id, CancellationToken ct);
    }
}
