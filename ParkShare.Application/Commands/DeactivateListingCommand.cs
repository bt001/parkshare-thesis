using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class DeactivateListingCommand : IRequest<ServiceResult<DeactivateListingResponse>>
    {
        public long Id { get; init; }
        public long OwnerId { get; init; }
    }

    public class DeactivateListingHandler : IRequestHandler<DeactivateListingCommand, ServiceResult<DeactivateListingResponse>>
    {
        private readonly IListingService _listingService;
        public DeactivateListingHandler(IListingService listingService) => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<DeactivateListingResponse>> Handle(DeactivateListingCommand request, CancellationToken cancellationToken)
        {
            var response = await _listingService.DeactivateAsync(request.Id, request.OwnerId, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<DeactivateListingResponse>.WithErrors(response.Errors);

            return ServiceResult<DeactivateListingResponse>.Success(response.Payload);
        }
    }

}
