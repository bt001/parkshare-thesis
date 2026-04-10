using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    /// <summary>
    /// Deletes an availability window.
    /// </summary>
    public class DeleteAvailabilityWindowCommand : IRequest<ServiceResult<DeleteAvailabilityWindowResponse>>
    {
        /// <summary>
        /// Availability window ID.
        /// </summary>
        public long Id { get; init; }
        /// <summary>
        /// Owner ID.
        /// </summary>
        public long OwnerId { get; init; }
    }

    /// <summary>
    /// Handles availability window deletion.
    /// </summary>
    public class DeleteAvailabilityWindowHandler : IRequestHandler<DeleteAvailabilityWindowCommand, ServiceResult<DeleteAvailabilityWindowResponse>>
    {
        private readonly IAvailabilityService _availabilityService;

        /// <summary>
        /// Creates the handler.
        /// </summary>
        public DeleteAvailabilityWindowHandler(IAvailabilityService availabilityService)
            => _availabilityService = availabilityService ?? throw new ArgumentNullException(nameof(availabilityService));

        /// <summary>
        /// Handles the command.
        /// </summary>
        public async Task<ServiceResult<DeleteAvailabilityWindowResponse>> Handle(DeleteAvailabilityWindowCommand request, CancellationToken cancellationToken)
        {
            var windowRes = await _availabilityService.GetWindowCoreAsync(request.Id, cancellationToken);
            if (!windowRes.IsSuccessful)
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithErrors(windowRes.Errors);

            if (windowRes.Payload.OwnerId != request.OwnerId)
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithError("Availability window does not belong to the specified owner.");

            var response = await _availabilityService.DeleteAsync(request.Id, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<DeleteAvailabilityWindowResponse>.WithErrors(response.Errors);

            return ServiceResult<DeleteAvailabilityWindowResponse>.Success(response.Payload);
        }
    }
}
