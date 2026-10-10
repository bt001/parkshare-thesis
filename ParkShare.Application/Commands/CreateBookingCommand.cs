using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class CreateBookingCommand : IRequest<ServiceResult<CreateBookingResponse>>
    {
        public long ListingId { get; init; }
        public long RenterId { get; init; }
        public DateTime StartDateTime { get; init; }
        public DateTime EndDateTime { get; init; }
    }

    /// <summary>Delegates booking validation and mutation to the transaction-owning service.</summary>
    public class CreateBookingHandler : IRequestHandler<CreateBookingCommand, ServiceResult<CreateBookingResponse>>
    {
        private readonly IBookingService _bookingService;

        /// <summary>Creates the booking handler.</summary>
        public CreateBookingHandler(IBookingService bookingService)
            => _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateBookingResponse>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            var result = await _bookingService.CreateAsync(request, cancellationToken);
            if (!result.IsSuccessful)
                return ServiceResult<CreateBookingResponse>.WithErrors(result);
            return ServiceResult<CreateBookingResponse>.Success(result.Payload);
        }
    }
}
