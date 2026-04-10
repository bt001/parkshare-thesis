using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class GetOwnerListingsQuery : IRequest<ServiceResult<GetOwnerListingsResponse>>
    {
        public long OwnerId { get; init; }
    }

    public class GetOwnerListingsHandler : IRequestHandler<GetOwnerListingsQuery, ServiceResult<GetOwnerListingsResponse>>
    {
        private readonly IListingService _listingService;
        public GetOwnerListingsHandler(IListingService listingService) => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<GetOwnerListingsResponse>> Handle(GetOwnerListingsQuery request, CancellationToken cancellationToken)
        {
            var response = await _listingService.GetByOwnerAsync(request.OwnerId, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<GetOwnerListingsResponse>.WithErrors(response.Errors);

            return ServiceResult<GetOwnerListingsResponse>.Success(response.Payload);
        }
    }
}
