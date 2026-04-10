using AutoMapper;
using ParkShare.Application.Commands;
using ParkShare.Application.Dto.Application;
using ParkShare.Application.Infrastructure.Automapper;
using ParkShare.Application.Queries;
using ParkShare.Shared.Requests.Availability;
using ParkShare.Shared.Results.Availability;

namespace ParkShare.Api.Mappings
{
    /// <summary>
    /// Configures availability API mappings.
    /// </summary>
    public class AvailabilityMappings : IHaveCustomMapping
    {
        /// <summary>
        /// Registers availability mappings.
        /// </summary>
        /// <param name="cfg">AutoMapper profile.</param>
        public void CreateMappings(Profile cfg)
        {
            cfg.CreateMap<CreateAvailabilityWindowRequest, CreateAvailabilityWindowCommand>();
            cfg.CreateMap<CreateAvailabilityWindowResponse, AvailabilityWindowMutationResult>();

            cfg.CreateMap<UpdateAvailabilityWindowRequest, UpdateAvailabilityWindowCommand>();
            cfg.CreateMap<UpdateAvailabilityWindowResponse, AvailabilityWindowMutationResult>();

            cfg.CreateMap<DeleteAvailabilityWindowRequest, DeleteAvailabilityWindowCommand>();
            cfg.CreateMap<DeleteAvailabilityWindowResponse, DeleteAvailabilityWindowResult>();

            cfg.CreateMap<GetListingAvailabilityRequest, GetListingAvailabilityQuery>();
            cfg.CreateMap<GetOwnerAvailabilityRequest, GetOwnerAvailabilityQuery>();
            cfg.CreateMap<GetAvailabilityWindowsResponse, GetAvailabilityWindowsResult>();
            cfg.CreateMap<AvailabilityWindowResponse, AvailabilityWindowResult>();
        }
    }
}
