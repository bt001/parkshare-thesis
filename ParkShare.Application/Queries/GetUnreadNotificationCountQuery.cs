using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>Retrieves the authenticated recipient unread count.</summary>
    public class GetUnreadNotificationCountQuery : IRequest<ServiceResult<UnreadNotificationCountResponse>>
    {
        /// <summary>Recipient identity supplied by the authenticated API.</summary>
        public long UserId { get; set; }
    }

    /// <summary>Delegates recipient-scoped notification operations to the application service.</summary>
    public class GetUnreadNotificationCountHandler : IRequestHandler<GetUnreadNotificationCountQuery, ServiceResult<UnreadNotificationCountResponse>>
    {
        private readonly INotificationService _service;
        /// <summary>Creates the notification handler.</summary>
        public GetUnreadNotificationCountHandler(INotificationService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<UnreadNotificationCountResponse>> Handle(GetUnreadNotificationCountQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.CountUnreadAsync(request.UserId, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<UnreadNotificationCountResponse>.WithErrors(result);
            return ServiceResult<UnreadNotificationCountResponse>.Success(result.Payload);
        }
    }
}
