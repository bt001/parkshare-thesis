using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class GetBookingByIdQuery : IRequest<ServiceResult<GetBookingByIdResponse>>
    {
        public long Id { get; init; }
    }

    public class GetBookingByIdHandler : IRequestHandler<GetBookingByIdQuery, ServiceResult<GetBookingByIdResponse>>
    {
        private readonly IBookingService _bookingService;

        public GetBookingByIdHandler(IBookingService bookingService)
          => _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

        public async Task<ServiceResult<GetBookingByIdResponse>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var result = await _bookingService.GetByIdAsync(request.Id, cancellationToken);
            if (!result.IsSuccessful)
                return ServiceResult<GetBookingByIdResponse>.WithErrors(result.Errors);

            return ServiceResult<GetBookingByIdResponse>.Success(result.Payload);
        }
    }

}
