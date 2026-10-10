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

        /// <summary>Creates the availability handler.</summary>
        public UpdateAvailabilityWindowHandler(IAvailabilityService availabilityService)
            => _availabilityService = availabilityService;

        /// <inheritdoc/>
        public async Task<ServiceResult<UpdateAvailabilityWindowResponse>> Handle(UpdateAvailabilityWindowCommand request, CancellationToken cancellationToken)
        {
            var result = await _availabilityService.UpdateAsync(request.Id, request.OwnerId, request.StartDateTime, request.EndDateTime, request.IsBlocked, request.Note, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<UpdateAvailabilityWindowResponse>.WithErrors(result);
            return ServiceResult<UpdateAvailabilityWindowResponse>.Success(result.Payload);
        }
    }
}
