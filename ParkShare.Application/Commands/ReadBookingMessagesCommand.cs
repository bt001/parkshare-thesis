using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands
{
    /// <summary>ReadBookingMessages participant operation.</summary>
    public class ReadBookingMessagesCommand : IRequest<ServiceResult<ReadMessagesResponse>>
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; set; }
        /// <summary>Authenticated participant identity.</summary>
        public long UserId { get; set; }
        /// <summary>Exact displayed message identities.</summary>
        public List<long>? MessageIds { get; set; }
    }
    /// <summary>Delegates the participant operation to the chat service.</summary>
    public class ReadBookingMessagesHandler : IRequestHandler<ReadBookingMessagesCommand, ServiceResult<ReadMessagesResponse>>
    {
        private readonly IChatService _service;
        /// <summary>Creates the chat handler.</summary>
        public ReadBookingMessagesHandler(IChatService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<ReadMessagesResponse>> Handle(ReadBookingMessagesCommand request, CancellationToken cancellationToken)
        {
            var result = await _service.MarkReadAsync(request.BookingId, request.UserId, request.MessageIds, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<ReadMessagesResponse>.WithErrors(result);
            return ServiceResult<ReadMessagesResponse>.Success(result.Payload);
        }
    }
}
