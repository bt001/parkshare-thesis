using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>GetBookingMessages participant operation.</summary>
    public class GetBookingMessagesQuery : IRequest<ServiceResult<GetBookingMessagesResponse>>
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; set; }
        /// <summary>Authenticated participant identity.</summary>
        public long UserId { get; set; }
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum messages per page.</summary>
        public int PageSize { get; set; } = 50;
    }
    /// <summary>Delegates the participant operation to the chat service.</summary>
    public class GetBookingMessagesHandler : IRequestHandler<GetBookingMessagesQuery, ServiceResult<GetBookingMessagesResponse>>
    {
        private readonly IChatService _service;
        /// <summary>Creates the chat handler.</summary>
        public GetBookingMessagesHandler(IChatService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<GetBookingMessagesResponse>> Handle(GetBookingMessagesQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.GetAsync(request.BookingId, request.UserId, request.Page, request.PageSize, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<GetBookingMessagesResponse>.WithErrors(result);
            return ServiceResult<GetBookingMessagesResponse>.Success(result.Payload);
        }
    }
}
