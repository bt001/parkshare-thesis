using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class RejectBookingCommand : IRequest<ServiceResult<RejectBookingResponse>>
    {
        public long BookingId { get; init; }
        public long OwnerId { get; init; }
        public string? Reason { get; init; }
    }

    /// <summary>Delegates booking validation and mutation to the transaction-owning service.</summary>
    public class RejectBookingHandler : IRequestHandler<RejectBookingCommand, ServiceResult<RejectBookingResponse>>
    {
        private readonly IBookingService _bookingService;

        /// <summary>Creates the booking handler.</summary>
        public RejectBookingHandler(IBookingService bookingService)
            => _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

        /// <inheritdoc/>
        public async Task<ServiceResult<RejectBookingResponse>> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
        {
            var result = await _bookingService.RejectAsync(request.BookingId, request.OwnerId, request.Reason, cancellationToken);
            if (!result.IsSuccessful)
                return ServiceResult<RejectBookingResponse>.WithErrors(result);
            return ServiceResult<RejectBookingResponse>.Success(result.Payload);
        }
    }
}
