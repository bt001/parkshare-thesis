using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
using ParkShare.Common.Enums;

namespace ParkShare.Application.Queries
{
    /// <summary>Retrieves the authenticated renter booking history.</summary>
    public class GetRenterBookingsQuery : IRequest<ServiceResult<GetRenterBookingsResponse>>
    {
        /// <summary>Renter ID, overwritten by the API from the authenticated user.</summary>
        public long RenterId { get; set; }
        /// <summary>Optional booking statuses.</summary>
        public List<BookingStatus>? Statuses { get; set; }
        /// <summary>One-based page number.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Maximum number of bookings per page.</summary>
        public int PageSize { get; set; } = 20;
    }

    /// <summary>Coordinates renter history retrieval.</summary>
    public class GetRenterBookingsHandler : IRequestHandler<GetRenterBookingsQuery, ServiceResult<GetRenterBookingsResponse>>
    {
        private readonly IBookingService _bookingService;

        /// <summary>Creates the renter history handler.</summary>
        public GetRenterBookingsHandler(IBookingService bookingService)
            => _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

        /// <inheritdoc/>
        public async Task<ServiceResult<GetRenterBookingsResponse>> Handle(GetRenterBookingsQuery request, CancellationToken cancellationToken)
        {
            var response = await _bookingService.GetRenterByStatusesAsync(request.RenterId, request.Statuses, request.Page, request.PageSize, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<GetRenterBookingsResponse>.WithErrors(response);

            return ServiceResult<GetRenterBookingsResponse>.Success(response.Payload);
        }
    }
}
