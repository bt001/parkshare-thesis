using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands
{
    /// <summary>CompleteBooking operation input.</summary>
    public class CompleteBookingCommand : IRequest<ServiceResult<CompleteBookingResponse>>
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; init; }
        /// <summary>Authenticated owner.</summary>
        public long OwnerId { get; init; }
    }
    /// <summary>Delegates validation and persistence to the application service.</summary>
    public class CompleteBookingHandler : IRequestHandler<CompleteBookingCommand, ServiceResult<CompleteBookingResponse>>
    {
        private readonly IBookingService _service;
        /// <summary>Creates the handler.</summary>
        public CompleteBookingHandler(IBookingService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<CompleteBookingResponse>> Handle(CompleteBookingCommand request, CancellationToken cancellationToken)
        {
            var result = await _service.CompleteAsync(request.BookingId, request.OwnerId, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<CompleteBookingResponse>.WithErrors(result);
            return ServiceResult<CompleteBookingResponse>.Success(result.Payload);
        }
    }
}
