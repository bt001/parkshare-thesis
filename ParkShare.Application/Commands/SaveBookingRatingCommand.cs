using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Commands
{
    /// <summary>SaveBookingRating operation input.</summary>
    public class SaveBookingRatingCommand : IRequest<ServiceResult<RatingResponse>>
    {
        /// <summary>Booking identity.</summary>
        public long BookingId { get; init; }
        /// <summary>Authenticated renter.</summary>
        public long RenterId { get; init; }
        /// <summary>Stars from one to five.</summary>
        public int Stars { get; init; }
        /// <summary>Optional plain-text comment.</summary>
        public string? Comment { get; init; }
    }
    /// <summary>Delegates validation and persistence to the application service.</summary>
    public class SaveBookingRatingHandler : IRequestHandler<SaveBookingRatingCommand, ServiceResult<RatingResponse>>
    {
        private readonly IRatingService _service;
        /// <summary>Creates the handler.</summary>
        public SaveBookingRatingHandler(IRatingService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<RatingResponse>> Handle(SaveBookingRatingCommand request, CancellationToken cancellationToken)
        {
            var result = await _service.SaveAsync(request.BookingId, request.RenterId, request.Stars, request.Comment, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<RatingResponse>.WithErrors(result);
            return ServiceResult<RatingResponse>.Success(result.Payload);
        }
    }
}
