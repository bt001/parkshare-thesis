using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Queries
{
    public class GetListingByIdQuery : IRequest<ServiceResult<GetListingByIdResponse>>
    {
        public long Id { get; init; }
    }

    public class GetListingByIdHandler : IRequestHandler<GetListingByIdQuery, ServiceResult<GetListingByIdResponse>>
    {
        private readonly IListingService _listingService;
        public GetListingByIdHandler(IListingService listingService)
            => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<GetListingByIdResponse>> Handle(GetListingByIdQuery request, CancellationToken cancellationToken)
        {
            var response = await _listingService.GetByIdAsync(request.Id, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<GetListingByIdResponse>.WithErrors(response);

            return ServiceResult<GetListingByIdResponse>.Success(response.Payload);
        }
    }
}
