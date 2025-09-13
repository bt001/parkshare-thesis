using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class ActivateListingCommand : IRequest<ServiceResult<ActivateListingResponse>>
    {
        public long Id { get; init; }
        public long OwnerId { get; init; }
    }

    public class ActivateListingHandler : IRequestHandler<ActivateListingCommand, ServiceResult<ActivateListingResponse>>
    {
        private readonly IListingService _listingService;
        public ActivateListingHandler(IListingService listingService) => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<ActivateListingResponse>> Handle(ActivateListingCommand request, CancellationToken cancellationToken)
        {
            var response = await _listingService.ActivateAsync(request.Id, request.OwnerId, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<ActivateListingResponse>.WithErrors(response.Errors);

            return ServiceResult<ActivateListingResponse>.Success(response.Payload);
        }
    }
}
