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

    public class RejectBookingHandler : IRequestHandler<RejectBookingCommand, ServiceResult<RejectBookingResponse>>
    {
        private readonly IBookingService _bookingService;

        public RejectBookingHandler(IBookingService bookingService)
          => _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

        public async Task<ServiceResult<RejectBookingResponse>> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
        {
            var ownerRes = await _bookingService.GetOwnerForBookingAsync(request.BookingId, cancellationToken);
            if (!ownerRes.IsSuccessful)
                return ServiceResult<RejectBookingResponse>.WithErrors(ownerRes.Errors);
            if (ownerRes.Payload != request.OwnerId)
                return ServiceResult<RejectBookingResponse>.WithError("Not authorized to reject this booking.");

            var reject = await _bookingService.RejectAsync(request.BookingId, request.Reason, cancellationToken);
            if (!reject.IsSuccessful)
                return ServiceResult<RejectBookingResponse>.WithErrors(reject.Errors);

            return ServiceResult<RejectBookingResponse>.Success(reject.Payload);
        }
    }

}
