using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;

namespace ParkShare.Application.Commands
{
    public class CreateListingCommand : IRequest<ServiceResult<CreateListingResponse>>
    {
        public long OwnerId { get; init; }
        /// <summary>Selected amenity IDs; null selects none on creation.</summary>
        public List<long>? AmenityIds { get; set; }

        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal PricePerHour { get; init; }
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        public string? Address { get; init; }
        /// <summary>
        /// Optional parking-space size or type.
        /// </summary>
        public string? Size { get; init; }
        /// <summary>
        /// Whether the listing is available except for blocked windows.
        /// </summary>
        public bool IsAlwaysAvailable { get; init; } = true;
    }


    public class CreateListingHandler : IRequestHandler<CreateListingCommand, ServiceResult<CreateListingResponse>>
    {
        private readonly IListingService _listingService;

        public CreateListingHandler(IListingService listingService)
            => _listingService = listingService ?? throw new ArgumentNullException(nameof(listingService));

        public async Task<ServiceResult<CreateListingResponse>> Handle(CreateListingCommand request, CancellationToken cancellationToken)
        {
            var response = await _listingService.CreateAsync(
                request.OwnerId,
                request.Title,
                request.Description,
                request.PricePerHour,
                request.Latitude,
                request.Longitude,
                request.Address,
                request.Size,
                request.IsAlwaysAvailable,
                cancellationToken, request.AmenityIds);

            if (!response.IsSuccessful)
                return ServiceResult<CreateListingResponse>.WithErrors(response);

            return ServiceResult<CreateListingResponse>.Success(response.Payload);
        }
    }

}
