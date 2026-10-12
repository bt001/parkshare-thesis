using MediatR;
using ParkShare.Application.Dto;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Interfaces;
namespace ParkShare.Application.Queries
{
    /// <summary>Requests the public amenity catalog.</summary>
    public class GetAmenitiesQuery : IRequest<ServiceResult<GetAmenitiesResponse>> { }
    /// <summary>Loads the catalog through the application service.</summary>
    public class GetAmenitiesHandler : IRequestHandler<GetAmenitiesQuery, ServiceResult<GetAmenitiesResponse>>
    {
        private readonly IAmenityService _service;
        /// <summary>Creates the catalog handler.</summary>
        public GetAmenitiesHandler(IAmenityService service) => _service = service;
        /// <inheritdoc/>
        public async Task<ServiceResult<GetAmenitiesResponse>> Handle(GetAmenitiesQuery request, CancellationToken cancellationToken)
        {
            var result = await _service.GetAllAsync(cancellationToken);
            if (!result.IsSuccessful) return ServiceResult<GetAmenitiesResponse>.WithErrors(result);
            return ServiceResult<GetAmenitiesResponse>.Success(result.Payload);
        }
    }
}
