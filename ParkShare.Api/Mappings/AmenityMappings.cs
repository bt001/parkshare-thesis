using AutoMapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Amenities;
using ParkShare.Shared.Results.Amenities;
namespace ParkShare.Api.Mappings
{
    /// <summary>Maps amenity API contracts.</summary>
    public class AmenityMappings : IHaveCustomMapping
    {
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<GetAmenitiesRequest, GetAmenitiesQuery>();
            cfg.CreateMap<GetAmenitiesResponse, GetAmenitiesResult>();
            cfg.CreateMap<AmenityResponse, AmenityResult>();
        }
    }
}
