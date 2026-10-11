using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands
{
    /// <summary>SendBookingMessage participant operation.</summary>
    public class SendBookingMessageCommand : IRequest<ServiceResult<ChatMessageResponse>>
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; set; }
        /// <summary>Authenticated participant identity.</summary>
        public long UserId { get; set; }
        /// <summary>Plain-text message.</summary>
        public string MessageText { get; set; } = string.Empty;
    }
    /// <summary>Delegates the participant operation to the chat service.</summary>
    public class SendBookingMessageHandler : IRequestHandler<SendBookingMessageCommand, ServiceResult<ChatMessageResponse>>
    {
        private readonly IChatService _service;
        /// <summary>Creates the chat handler.</summary>
        public SendBookingMessageHandler(IChatService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<ChatMessageResponse>> Handle(SendBookingMessageCommand request, CancellationToken cancellationToken)
        {
            var result = await _service.SendAsync(request.BookingId, request.UserId, request.MessageText, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<ChatMessageResponse>.WithErrors(result);
            return ServiceResult<ChatMessageResponse>.Success(result.Payload);
        }
    }
}
