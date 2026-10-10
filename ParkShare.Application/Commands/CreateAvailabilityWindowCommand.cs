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

        /// <summary>Creates the availability handler.</summary>
        public CreateAvailabilityWindowHandler(IAvailabilityService availabilityService)
            => _availabilityService = availabilityService;

        /// <inheritdoc/>
        public async Task<ServiceResult<CreateAvailabilityWindowResponse>> Handle(CreateAvailabilityWindowCommand request, CancellationToken cancellationToken)
        {
            var result = await _availabilityService.CreateAsync(request.ListingId, request.OwnerId, request.StartDateTime, request.EndDateTime, request.IsBlocked, request.Note, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<CreateAvailabilityWindowResponse>.WithErrors(result);
            return ServiceResult<CreateAvailabilityWindowResponse>.Success(result.Payload);
        }
    }
}
