using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class ApproveBookingCommand : IRequest<ServiceResult<ApproveBookingResponse>>
    {
        public long BookingId { get; init; }
        public long OwnerId { get; init; }
    }

    /// <summary>Delegates booking validation and mutation to the transaction-owning service.</summary>
    public class ApproveBookingHandler : IRequestHandler<ApproveBookingCommand, ServiceResult<ApproveBookingResponse>>
    {
        private readonly IBookingService _bookingService;

        /// <summary>Creates the booking handler.</summary>
        public ApproveBookingHandler(IBookingService bookingService)
            => _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

        /// <inheritdoc/>
        public async Task<ServiceResult<ApproveBookingResponse>> Handle(ApproveBookingCommand request, CancellationToken cancellationToken)
        {
            var result = await _bookingService.ApproveAndRejectOverlappingPendingAsync(request.BookingId, request.OwnerId, cancellationToken);
            if (!result.IsSuccessful)
                return ServiceResult<ApproveBookingResponse>.WithErrors(result);
            return ServiceResult<ApproveBookingResponse>.Success(result.Payload);
        }
    }
}
