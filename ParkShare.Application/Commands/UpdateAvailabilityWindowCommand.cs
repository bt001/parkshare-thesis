using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    /// <summary>
    /// Updates an availability window.
    /// </summary>
    public class UpdateAvailabilityWindowCommand : IRequest<ServiceResult<UpdateAvailabilityWindowResponse>>
    {
        /// <summary>
        /// Availability window ID.
        /// </summary>
        public long Id { get; init; }
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; init; }
        /// <summary>
        /// Updated start time.
        /// </summary>
        public DateTime StartDateTime { get; init; }
        /// <summary>
        /// Updated end time.
        /// </summary>
        public DateTime EndDateTime { get; init; }
        /// <summary>
        /// Updated blocked flag.
        /// </summary>
        public bool IsBlocked { get; init; }
        /// <summary>
        /// Updated note.
        /// </summary>
        public string? Note { get; init; }
    }

    /// <summary>
    /// Handles availability window updates.
    /// </summary>
    public class UpdateAvailabilityWindowHandler : IRequestHandler<UpdateAvailabilityWindowCommand, ServiceResult<UpdateAvailabilityWindowResponse>>
    {
        private readonly IAvailabilityService _availabilityService;
        private readonly IBookingService _bookingService;
        private readonly IListingReadService _listingReadService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        public UpdateAvailabilityWindowHandler(
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
        public async Task<ServiceResult<UpdateAvailabilityWindowResponse>> Handle(UpdateAvailabilityWindowCommand request, CancellationToken cancellationToken)
        {
            var windowRes = await _availabilityService.GetWindowCoreAsync(request.Id, cancellationToken);
            if (!windowRes.IsSuccessful)
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithErrors(windowRes.Errors);

            if (windowRes.Payload.OwnerId != request.OwnerId)
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Availability window does not belong to the specified owner.");

            var listingRes = await _listingReadService.GetCoreAsync(windowRes.Payload.ListingId, cancellationToken);
            if (!listingRes.IsSuccessful)
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithErrors(listingRes.Errors);

            if (!request.IsBlocked && listingRes.Payload.IsAlwaysAvailable)
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Always-available listings cannot add open availability windows.");

            if (request.IsBlocked)
            {
                var confirmedOverlap = await _bookingService.HasConfirmedOverlapAsync(windowRes.Payload.ListingId, request.StartDateTime, request.EndDateTime, cancellationToken);
                if (!confirmedOverlap.IsSuccessful)
                    return ServiceResult<UpdateAvailabilityWindowResponse>.WithErrors(confirmedOverlap.Errors);

                if (confirmedOverlap.Payload)
                    return ServiceResult<UpdateAvailabilityWindowResponse>.WithError("Blocked windows cannot overlap confirmed bookings.");
            }

            var response = await _availabilityService.UpdateAsync(
                request.Id,
                request.StartDateTime,
                request.EndDateTime,
                request.IsBlocked,
                request.Note,
                cancellationToken);

            if (!response.IsSuccessful)
                return ServiceResult<UpdateAvailabilityWindowResponse>.WithErrors(response.Errors);

            return ServiceResult<UpdateAvailabilityWindowResponse>.Success(response.Payload);
        }
    }
}
