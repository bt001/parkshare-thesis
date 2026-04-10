using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class UpdateListingCommand : IRequest<ServiceResult<UpdateListingResponse>>
    {
        public long Id { get; init; }
        public long OwnerId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal PricePerHour { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string? Address { get; init; }
        public string? Size { get; init; }
        public bool IsAlwaysAvailable { get; init; }
    }

    public class UpdateListingHandler : IRequestHandler<UpdateListingCommand, ServiceResult<UpdateListingResponse>>
    {
        private readonly IListingService _listingService;
        public UpdateListingHandler(IListingService listingService) => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<UpdateListingResponse>> Handle(UpdateListingCommand request, CancellationToken cancellationToken)
        {
            var response = await _listingService.UpdateAsync(request.Id, request.OwnerId, request.Title, request.Description, request.PricePerHour, request.Latitude, request.Longitude, request.Address, request.Size, request.IsAlwaysAvailable, cancellationToken);
            if (!response.IsSuccessful)
                return ServiceResult<UpdateListingResponse>.WithErrors(response.Errors);

            return ServiceResult<UpdateListingResponse>.Success(response.Payload);
        }
    }
}
