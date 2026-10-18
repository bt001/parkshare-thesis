using AutoMapper;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Places;
using ParkShare.Shared.Results.Places;
namespace ParkShare.Api.Mappings
{
    /// <summary>Maps place search API contracts.</summary>
    public class PlaceMappings : IHaveCustomMapping
    {
        /// <inheritdoc/>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<SearchPlacesRequest, SearchPlacesQuery>();
            cfg.CreateMap<SearchPlacesResponse, SearchPlacesResult>();
            cfg.CreateMap<PlaceResponse, PlaceResult>();
        }
    }
}
