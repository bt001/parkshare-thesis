using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands
{
    /// <summary>Marks an authenticated recipient notification read.</summary>
    public class MarkNotificationReadCommand : IRequest<ServiceResult<MarkNotificationReadResponse>>
    {
        /// <summary>Recipient identity supplied by the authenticated API.</summary>
        public long UserId { get; set; }
        /// <summary>Notification identity.</summary>
        public long Id { get; set; }
    }

    /// <summary>Delegates recipient-scoped notification operations to the application service.</summary>
    public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand, ServiceResult<MarkNotificationReadResponse>>
    {
        private readonly INotificationService _service;
        /// <summary>Creates the notification handler.</summary>
        public MarkNotificationReadHandler(INotificationService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<MarkNotificationReadResponse>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var result = await _service.MarkReadAsync(request.UserId, request.Id, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<MarkNotificationReadResponse>.WithErrors(result);
            return ServiceResult<MarkNotificationReadResponse>.Success(result.Payload);
        }
    }
}
