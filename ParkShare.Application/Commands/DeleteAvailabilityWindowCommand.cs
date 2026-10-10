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

        /// <summary>Creates the availability handler.</summary>
        public DeleteAvailabilityWindowHandler(IAvailabilityService availabilityService)
            => _availabilityService = availabilityService;

        /// <inheritdoc/>
        public async Task<ServiceResult<DeleteAvailabilityWindowResponse>> Handle(DeleteAvailabilityWindowCommand request, CancellationToken cancellationToken)
        {
            var result = await _availabilityService.DeleteAsync(request.Id, request.OwnerId, cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<DeleteAvailabilityWindowResponse>.WithErrors(result);
            return ServiceResult<DeleteAvailabilityWindowResponse>.Success(result.Payload);
        }
    }
}
