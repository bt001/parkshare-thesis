using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>Retrieves the authenticated recipient inbox.</summary>
    public class GetNotificationsQuery : IRequest<ServiceResult<GetNotificationsResponse>>
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

    /// <summary>Delegates recipient-scoped notification operations to the application service.</summary>
    public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, ServiceResult<GetNotificationsResponse>>
    {
        private readonly INotificationService _service;
        /// <summary>Creates the notification handler.</summary>
        public GetNotificationsHandler(INotificationService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<GetNotificationsResponse>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.GetAsync(request.UserId, request.UnreadOnly, request.Page, request.PageSize, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<GetNotificationsResponse>.WithErrors(result);
            return ServiceResult<GetNotificationsResponse>.Success(result.Payload);
        }
    }
}
