using Microsoft.Extensions.Logging;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Application.Interfaces.Repositories;
namespace ParkShare.Application.Services
{
    /// <inheritdoc/>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly ILogger<NotificationService> _logger;
        /// <summary>Creates the recipient inbox service.</summary>
        public NotificationService(INotificationRepository repository, ILogger<NotificationService> logger)
        { _repository = repository; _logger = logger; }

        /// <inheritdoc/>
        public Task<ServiceResult<GetNotificationsResponse>> GetAsync(long userId, bool unreadOnly, int page, int pageSize, CancellationToken ct)
            => ExecuteAsync(async () =>
            {
                var result = await _repository.GetAsync(userId, unreadOnly, page, pageSize, ct);
                return ServiceResult<GetNotificationsResponse>.Success(new()
                {
                    Page = page, PageSize = pageSize, TotalCount = result.TotalCount,
                    Items = result.Items.Select(n => new NotificationItemResponse
                    {
                        Id = n.Id, BookingId = n.BookingId, Type = n.Type, Message = n.Message,
                        IsRead = n.IsRead, CreatedAt = DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)
                    }).ToList()
                });
            }, userId, ct);

        /// <inheritdoc/>
        public Task<ServiceResult<UnreadNotificationCountResponse>> CountUnreadAsync(long userId, CancellationToken ct)
            => ExecuteAsync(async () => ServiceResult<UnreadNotificationCountResponse>.Success(new()
                { Count = await _repository.CountUnreadAsync(userId, ct) }), userId, ct);

        /// <inheritdoc/>
        public Task<ServiceResult<MarkNotificationReadResponse>> MarkReadAsync(long userId, long id, CancellationToken ct)
            => ExecuteAsync(async () => await _repository.MarkReadAsync(userId, id, ct)
                ? ServiceResult<MarkNotificationReadResponse>.Success(new() { IsRead = true })
                : ServiceResult<MarkNotificationReadResponse>.WithError("Notification not found."), userId, ct);

        /// <summary>Logs persistence failures without hiding caller cancellation.</summary>
        private async Task<ServiceResult<T>> ExecuteAsync<T>(Func<Task<ServiceResult<T>>> operation, long userId, CancellationToken ct) where T : notnull
        {
            try { return await operation(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification operation failed for recipient {UserId}.", userId);
                return ServiceResult<T>.WithError("Notifications could not be updated or loaded. Please try again.");
            }
        }
    }
}
