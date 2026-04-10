using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    /// <summary>
    /// Creates an availability window for a listing.
    /// </summary>
    public class CreateAvailabilityWindowCommand : IRequest<ServiceResult<CreateAvailabilityWindowResponse>>
    {
        /// <summary>
        /// Listing ID.
        /// </summary>
        public long ListingId { get; init; }
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; init; }
        /// <summary>
        /// Window start time.
        /// </summary>
        public DateTime StartDateTime { get; init; }
        /// <summary>
        /// Window end time.
        /// </summary>
        public DateTime EndDateTime { get; init; }
        /// <summary>
        /// Whether the window blocks availability.
        /// </summary>
        public bool IsBlocked { get; init; }
        /// <summary>
        /// Optional note.
        /// </summary>
        public string? Note { get; init; }
    }

    /// <summary>
    /// Handles availability window creation.
    /// </summary>
    public class CreateAvailabilityWindowHandler : IRequestHandler<CreateAvailabilityWindowCommand, ServiceResult<CreateAvailabilityWindowResponse>>
    {
        private readonly IAvailabilityService _availabilityService;
        private readonly IBookingService _bookingService;
        private readonly IListingReadService _listingReadService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        public CreateAvailabilityWindowHandler(
            IAvailabilityService availabilityService,
            IBookingService bookingService,
            IListingReadService listingReadService)
            => (_availabilityService, _bookingService, _listingReadService) = (
                availabilityService ?? throw new ArgumentNullException(nameof(availabilityService)),
                bookingService ?? throw new ArgumentNullException(nameof(bookingService)),
                listingReadService ?? throw new ArgumentNullException(nameof(listingReadService)));

        /// <summary>
        /// Handles the command.
        /// </summary>
        public async Task<ServiceResult<CreateAvailabilityWindowResponse>> Handle(CreateAvailabilityWindowCommand request, CancellationToken cancellationToken)
        {
            var listingRes = await _listingReadService.GetCoreAsync(request.ListingId, cancellationToken);
            if (!listingRes.IsSuccessful)
                return ServiceResult<CreateAvailabilityWindowResponse>.WithErrors(listingRes.Errors);

            if (listingRes.Payload.OwnerId != request.OwnerId)
                return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Listing does not belong to the specified owner.");

            if (!request.IsBlocked && listingRes.Payload.IsAlwaysAvailable)
                return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Always-available listings cannot add open availability windows.");

            if (request.IsBlocked)
            {
                var confirmedOverlap = await _bookingService.HasConfirmedOverlapAsync(request.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
                if (!confirmedOverlap.IsSuccessful)
                    return ServiceResult<CreateAvailabilityWindowResponse>.WithErrors(confirmedOverlap.Errors);

                if (confirmedOverlap.Payload)
                    return ServiceResult<CreateAvailabilityWindowResponse>.WithError("Blocked windows cannot overlap confirmed bookings.");
            }

            var response = await _availabilityService.CreateAsync(
                request.ListingId,
                request.StartDateTime,
                request.EndDateTime,
                request.IsBlocked,
                request.Note,
                cancellationToken);

            if (!response.IsSuccessful)
                return ServiceResult<CreateAvailabilityWindowResponse>.WithErrors(response.Errors);

            return ServiceResult<CreateAvailabilityWindowResponse>.Success(response.Payload);
        }
    }
}
